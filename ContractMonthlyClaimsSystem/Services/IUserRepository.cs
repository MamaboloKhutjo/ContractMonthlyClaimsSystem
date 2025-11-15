using ContractMonthlyClaimsSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ContractMonthlyClaimsSystem.Services
{
    public interface IUserRepository
    {
        Task<List<User>> GetUsersAsync();
        Task<User> GetUserByIdAsync(int id);
        Task<User> GetUserByCredentialsAsync(string username, string password);
        Task AddUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task DeleteUserAsync(int id);
        User CurrentUser { get; set; }
    }
}
