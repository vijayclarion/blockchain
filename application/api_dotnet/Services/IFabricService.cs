using ClaimsApi.Models;

namespace ClaimsApi.Services
{
    public interface IFabricService
    {
        Task<List<Claim>> GetAllClaimsAsync();
        Task<Claim> GetClaimAsync(string id);
        Task<string> CreateClaimAsync(CreateClaimDto dto);
        Task<string> ApproveClaimAsync(string id);
        Task<string> SettleClaimAsync(string id);
        Task InitLedgerAsync();
    }
}
