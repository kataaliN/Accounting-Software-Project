using System.Linq;
using FirstClassFinance.Interfaces;
using FirstClassFinance.Models;

namespace FirstClassFinance.Filters;

public class BaseAccountFilter : IAccountFilter
{
    public IQueryable<AccountModel> Apply(IQueryable<AccountModel> query)
    {
        return query;
    }
}