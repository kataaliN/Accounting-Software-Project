using System.Linq;
using FirstClassFinance.Interfaces;
using FirstClassFinance.Models;

namespace FirstClassFinance.Filters;

public class NameFilter : AccountFilterDecorator
{
    private readonly string _name;
    
    public NameFilter(IAccountFilter innerFilter, string name) : base(innerFilter)
    {
        _name = name;
    }

    public override IQueryable<AccountModel> Apply(IQueryable<AccountModel> query)
    {
        query = base.Apply(query);
        if (!string.IsNullOrEmpty(_name))
        {
            query = query.Where(a => a.AccountName.Contains(_name));
        }
        return query;
    }
}

public class CategoryFilter : AccountFilterDecorator
{
    private readonly string _category;

    public CategoryFilter(IAccountFilter innerFilter, string category) : base(innerFilter)
    {
        _category = category;
    }

    public override IQueryable<AccountModel> Apply(IQueryable<AccountModel> query)
    {
        query = base.Apply(query);
        if (!string.IsNullOrEmpty(_category))
        {
            query = query.Where(a => a.Category == _category);
        }
        return query;
    }
}

public class SubCategoryFilter : AccountFilterDecorator
{
    private readonly string _subcategory;

    public SubCategoryFilter(IAccountFilter innerFilter, string subcategory) : base(innerFilter)
    {
        _subcategory = subcategory;
    }

    public override IQueryable<AccountModel> Apply(IQueryable<AccountModel> query)
    {
        query = base.Apply(query);
        if (!string.IsNullOrEmpty(_subcategory))
        {
            query = query.Where(a=> a.Subcategory == _subcategory);
        }
        return query;
    }
}

public class BalanceFilter : AccountFilterDecorator
{
    private readonly decimal? _balance;

    public BalanceFilter(IAccountFilter innerFilter, decimal? balance) : base(innerFilter)
    {
        _balance = balance;
    }

    public override IQueryable<AccountModel> Apply(IQueryable<AccountModel> query)
    {
        query = base.Apply(query);
        if (_balance.HasValue)
        {
            query = query.Where(a => a.Balance >= _balance);
        }
        return query;
    }
}

public class NormalSideFilter : AccountFilterDecorator
{
    private readonly string? _normalSide;

    public NormalSideFilter(IAccountFilter innerFilter, string? normalSide) : base(innerFilter)
    {
        _normalSide = normalSide;
    }
    
    public override IQueryable<AccountModel> Apply(IQueryable<AccountModel> query)
    {
        query = base.Apply(query);
        if (!string.IsNullOrEmpty(_normalSide))
        {
            query = query.Where(a => a.NormalSide == _normalSide);
        }
        return query;
    }
}

public class OrderFilter : AccountFilterDecorator
{
    private readonly int? _order;

    public OrderFilter(IAccountFilter innerFilter, int? order) : base(innerFilter)
    {
        _order = order;
    }
    
    public override IQueryable<AccountModel> Apply(IQueryable<AccountModel> query)
    {
        query = base.Apply(query);
        if (_order.HasValue)
        {
            query = query.Where(a => a.Order == _order.Value);
        }
        return query;
    }
}

public class StatementFilter : AccountFilterDecorator
{
    private readonly string? _statementType;

    public StatementFilter(IAccountFilter innerFilter, string? statementType) : base(innerFilter)
    {
        _statementType = statementType;
    }
    
    public override IQueryable<AccountModel> Apply(IQueryable<AccountModel> query)
    {
        query = base.Apply(query);
        if (!string.IsNullOrEmpty(_statementType))
        {
            query = query.Where(a => a.Statement == _statementType);
        }
        return query;
    }   
}

public class DebitFilter : AccountFilterDecorator
{
    private readonly decimal? _debit;

    public DebitFilter(IAccountFilter innerFilter, decimal? debit) : base(innerFilter)
    {
        _debit = debit;
    }

    public override IQueryable<AccountModel> Apply(IQueryable<AccountModel> query)
    {
        query = base.Apply(query);
        if (_debit.HasValue)
        {
            query = query.Where(a => a.Debit >= _debit);
        }
        return query;
    }
}

public class CreditFilter : AccountFilterDecorator
{
    private readonly decimal? _credit;
    
    public CreditFilter(IAccountFilter innerFilter, decimal? credit) : base(innerFilter)
    {
        _credit = credit;
    }

    public override IQueryable<AccountModel> Apply(IQueryable<AccountModel> query)
    {
        query = base.Apply(query);
        if (_credit.HasValue)
        {
            query = query.Where(a => a.Credit >= _credit);
        }
        return query;
    }
}
// add filters for the remaining account fields
// add order, statement type, debit, credit, and normal side filters