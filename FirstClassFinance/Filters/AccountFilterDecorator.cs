using System.Linq;
using FirstClassFinance.Interfaces;
using FirstClassFinance.Models;

namespace FirstClassFinance.Filters;

public abstract class AccountFilterDecorator : IAccountFilter
{
    protected readonly IAccountFilter InnerFilter;

    protected AccountFilterDecorator(IAccountFilter innerFilter)
    {
        InnerFilter = innerFilter;
    }
    
    public virtual IQueryable<AccountModel> Apply(IQueryable<AccountModel> query)
    {
        InnerFilter.Apply(query);
        return query;
    }
}