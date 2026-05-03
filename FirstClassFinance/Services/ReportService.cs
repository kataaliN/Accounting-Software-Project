using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstClassFinance.Data;
using FirstClassFinance.Interfaces;
using FirstClassFinance.Models;
using Microsoft.EntityFrameworkCore;

namespace FirstClassFinance.Services
{
    public class ReportService
    {
        private readonly AppDBContext _context;
        private readonly INotificationService _notificationService;

        public ReportService(AppDBContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task SendReportNotification(string email, string reportName, DateTime asOfDate)
        {
            try
            {
                await _notificationService.EmailUser(email, $"{reportName} Generated",
                    $"The {reportName} as of {asOfDate:yyyy-MM-dd} has been successfully generated and is available for viewing.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to send report notification: {ex.Message}");
            }
        }

        public async Task<TrialBalanceModel> GetTrialBalance(DateTime? startDate, DateTime? endDate)
        {
            var accounts = await _context.Accounts.Where(a => a.IsActive).ToListAsync();
            var trialBalanceLines = new List<TrialBalanceLine>();

            foreach (var account in accounts)
            {
                var data = await GetAccountPeriodData(account.Id, startDate, endDate);
                if (data.Debit != 0 || data.Credit != 0 || data.EndingBalance != 0)
                {
                    trialBalanceLines.Add(new TrialBalanceLine
                    {
                        AccountId = account.Id,
                        AccountName = account.AccountName,
                        Debit = data.Debit,
                        Credit = data.Credit
                    });
                }
            }

            return new TrialBalanceModel
            {
                Lines = trialBalanceLines,
                TotalDebit = trialBalanceLines.Sum(l => l.Debit),
                TotalCredit = trialBalanceLines.Sum(l => l.Credit),
                AsOfDate = endDate ?? DateTime.UtcNow
            };
        }

        public async Task<IncomeStatementModel> GetIncomeStatement(DateTime? startDate, DateTime? endDate)
        {
            var accounts = await _context.Accounts
                .Where(a => a.IsActive && (a.Category == "Revenue" || a.Category == "Expense"))
                .ToListAsync();

            var revenues = new List<FinancialReportLine>();
            var expenses = new List<FinancialReportLine>();

            foreach (var account in accounts)
            {
                var data = await GetAccountPeriodData(account.Id, startDate, endDate);
                var amount = data.EndingBalance;
                
                if (account.Category == "Revenue")
                {
                    revenues.Add(new FinancialReportLine { AccountName = account.AccountName, Amount = amount });
                }
                else
                {
                    expenses.Add(new FinancialReportLine { AccountName = account.AccountName, Amount = amount });
                }
            }

            decimal totalRevenue = revenues.Sum(r => r.Amount);
            decimal totalExpense = expenses.Sum(e => e.Amount);

            return new IncomeStatementModel
            {
                Revenues = revenues,
                Expenses = expenses,
                TotalRevenue = totalRevenue,
                TotalExpense = totalExpense,
                NetIncome = totalRevenue - totalExpense,
                StartDate = startDate,
                EndDate = endDate ?? DateTime.UtcNow
            };
        }

        public async Task<BalanceSheetModel> GetBalanceSheet(DateTime? endDate)
        {
            var accounts = await _context.Accounts
                .Where(a => a.IsActive && (a.Category == "Asset" || a.Category == "Liability" || a.Category == "Equity"))
                .ToListAsync();

            var assets = new List<FinancialReportLine>();
            var liabilities = new List<FinancialReportLine>();
            var equity = new List<FinancialReportLine>();

            foreach (var account in accounts)
            {
                var data = await GetAccountPeriodData(account.Id, null, endDate);
                var balance = data.EndingBalance;

                if (account.Category == "Asset")
                    assets.Add(new FinancialReportLine { AccountName = account.AccountName, Amount = balance });
                else if (account.Category == "Liability")
                    liabilities.Add(new FinancialReportLine { AccountName = account.AccountName, Amount = balance });
                else if (account.Category == "Equity")
                    equity.Add(new FinancialReportLine { AccountName = account.AccountName, Amount = balance });
            }

            // Calculate Net Income for Retained Earnings
            var incomeStatement = await GetIncomeStatement(null, endDate);
            equity.Add(new FinancialReportLine { AccountName = "Retained Earnings (Net Income)", Amount = incomeStatement.NetIncome });

            return new BalanceSheetModel
            {
                Assets = assets,
                Liabilities = liabilities,
                Equity = equity,
                TotalAssets = assets.Sum(a => a.Amount),
                TotalLiabilities = liabilities.Sum(l => l.Amount),
                TotalEquity = equity.Sum(e => e.Amount),
                AsOfDate = endDate ?? DateTime.UtcNow
            };
        }

        public async Task<FinancialRatiosModel> GetFinancialRatios(DateTime? endDate)
        {
            var bs = await GetBalanceSheet(endDate);
            var isRep = await GetIncomeStatement(null, endDate);

            decimal currentAssets = bs.Assets.Where(a => a.AccountName.Contains("Current", StringComparison.OrdinalIgnoreCase)).Sum(a => a.Amount);
            if (currentAssets == 0) currentAssets = bs.TotalAssets; // Fallback

            decimal currentLiabilities = bs.Liabilities.Where(l => l.AccountName.Contains("Current", StringComparison.OrdinalIgnoreCase)).Sum(l => l.Amount);
            if (currentLiabilities == 0) currentLiabilities = bs.TotalLiabilities;

            return new FinancialRatiosModel
            {
                CurrentRatio = currentLiabilities != 0 ? currentAssets / currentLiabilities : 0,
                NetProfitMargin = isRep.TotalRevenue != 0 ? isRep.NetIncome / isRep.TotalRevenue : 0,
                DebtToEquityRatio = bs.TotalEquity != 0 ? bs.TotalLiabilities / bs.TotalEquity : 0,
                ReturnOnAssets = bs.TotalAssets != 0 ? isRep.NetIncome / bs.TotalAssets : 0,
                AsOfDate = endDate ?? DateTime.UtcNow
            };
        }

        private async Task<(decimal Debit, decimal Credit, decimal EndingBalance)> GetAccountPeriodData(int accountId, DateTime? start, DateTime? end)
        {
            var entries = _context.LedgerEntries.Where(e => e.accountId == accountId);
            if (start.HasValue) entries = entries.Where(e => e.date >= start.Value);
            if (end.HasValue) entries = entries.Where(e => e.date <= end.Value);

            var list = await entries.ToListAsync();
            decimal dr = list.Sum(e => e.debits);
            decimal cr = list.Sum(e => e.credits);
            
            // Get balance at end of period
            var lastEntry = await _context.LedgerEntries
                .Where(e => e.accountId == accountId && (!end.HasValue || e.date <= end.Value))
                .OrderByDescending(e => e.date)
                .ThenByDescending(e => e.ledgerId)
                .FirstOrDefaultAsync();

            decimal balance = lastEntry?.balance ?? 0;

            return (dr, cr, balance);
        }
    }

    public class TrialBalanceModel
    {
        public List<TrialBalanceLine> Lines { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public DateTime AsOfDate { get; set; }
    }

    public class TrialBalanceLine
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }

    public class IncomeStatementModel
    {
        public List<FinancialReportLine> Revenues { get; set; }
        public List<FinancialReportLine> Expenses { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetIncome { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class BalanceSheetModel
    {
        public List<FinancialReportLine> Assets { get; set; }
        public List<FinancialReportLine> Liabilities { get; set; }
        public List<FinancialReportLine> Equity { get; set; }
        public decimal TotalAssets { get; set; }
        public decimal TotalLiabilities { get; set; }
        public decimal TotalEquity { get; set; }
        public DateTime AsOfDate { get; set; }
    }

    public class FinancialReportLine
    {
        public string AccountName { get; set; }
        public decimal Amount { get; set; }
    }

    public class FinancialRatiosModel
    {
        public decimal CurrentRatio { get; set; }
        public decimal NetProfitMargin { get; set; }
        public decimal DebtToEquityRatio { get; set; }
        public decimal ReturnOnAssets { get; set; }
        public DateTime AsOfDate { get; set; }
    }
}
