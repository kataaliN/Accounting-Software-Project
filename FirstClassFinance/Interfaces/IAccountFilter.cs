using System.Linq;
using FirstClassFinance.Models;

namespace FirstClassFinance.Interfaces;

public interface IAccountFilter
{
    IQueryable<AccountModel> Apply(IQueryable<AccountModel> query);
}