using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;
using ClaimsApi.Models;

namespace ClaimsApi.Services
{
    public class CliFabricService : IFabricService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CliFabricService> _logger;

        public CliFabricService(IConfiguration configuration, ILogger<CliFabricService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<Claim>> GetAllClaimsAsync()
        {
            var result = await RunPeerCommand("query", "GetAllClaims", new string[] { });
            return JsonConvert.DeserializeObject<List<Claim>>(result) ?? new List<Claim>();
        }

        public async Task<Claim> GetClaimAsync(string id)
        {
            var result = await RunPeerCommand("query", "ReadClaim", new string[] { id });
            return JsonConvert.DeserializeObject<Claim>(result);
        }

        public async Task<string> CreateClaimAsync(CreateClaimDto dto)
        {
            // func CreateClaim(ctx, id, policyID, claimantName, amount (string/float), description, approverMSP)
            var amountStr = dto.Amount.ToString();
            var args = new[] { dto.ID, dto.PolicyID, dto.ClaimantName, amountStr, dto.Description, dto.ApproverMSP };
            return await RunPeerCommand("invoke", "CreateClaim", args);
        }

        public async Task<string> ApproveClaimAsync(string id)
        {
            return await RunPeerCommand("invoke", "ApproveClaim", new[] { id });
        }

        public async Task<string> SettleClaimAsync(string id)
        {
            return await RunPeerCommand("invoke", "SettleClaim", new[] { id });
        }

        public async Task InitLedgerAsync()
        {
            await RunPeerCommand("invoke", "InitLedger", new string[] { });
        }

        private async Task<string> RunPeerCommand(string method, string function, string[] args)
        {
            var peerBin = _configuration["Fabric:PeerBinaryPath"] ?? "peer";
            var channel = _configuration["Fabric:ChannelName"] ?? "mychannel";
            var chaincode = _configuration["Fabric:ChaincodeName"] ?? "basic";
            var mspId = _configuration["Fabric:MspId"] ?? "Org1MSP";
            var mspConfig = _configuration["Fabric:MspConfigPath"];
            var orderer = _configuration["Fabric:OrdererAddress"] ?? "localhost:7050";
            var peerAddress = _configuration["Fabric:PeerAddress"] ?? "localhost:7051";
            var tls = _configuration["Fabric:TlsEnabled"] ?? "false";
            
            // Construct JSON args array: '["CreateClaim", "ID", ...]'
            // Note: peer CLI expects '{"Args":["Func","Arg1"]}' or simple usage '{"Args":...}'
            // The standard 'peer chaincode invoke -c channel -n cc -c '{"Args":["Func","Arg"]}'
            
            var argsList = new List<string> { function };
            argsList.AddRange(args);
            var argsJson = JsonConvert.SerializeObject(new { Args = argsList });

            // Escape quotes for shell
            argsJson = argsJson.Replace("\"", "\\\"");

            var arguments = $"chaincode {method} -o {orderer} -C {channel} -n {chaincode} -c \"{argsJson}\"";

            if (Convert.ToBoolean(tls))
            {
                var caFile = _configuration["Fabric:OrdererCaFile"];
                arguments += $" --tls --cafile {caFile}";
            }

            // If we are using the 'peer' locally, we might need --peerAddresses for Invoke if we want to target specific peers
            // But simple 'peer chaincode invoke' tries to discover. 
            // However, with custom network, explicit target often needed if discovery not set.
            // Simplified for POC: assume one peer, no explicit targets unless needed.

            var startInfo = new ProcessStartInfo
            {
                FileName = peerBin,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Set Environment Variables for the CLI
            startInfo.EnvironmentVariables["CORE_PEER_LOCALMSPID"] = mspId;
            startInfo.EnvironmentVariables["CORE_PEER_MSPCONFIGPATH"] = mspConfig;
            startInfo.EnvironmentVariables["CORE_PEER_ADDRESS"] = peerAddress;
            // Additional typical vars
            startInfo.EnvironmentVariables["CORE_PEER_TLS_ENABLED"] = tls;
            startInfo.EnvironmentVariables["FABRIC_LOGGING_SPEC"] = "error";

            _logger.LogInformation($"Executing: {peerBin} {arguments}");

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError($"Peer CLI Error: {stderr}");
                throw new Exception($"Fabric Error: {stderr}");
            }

            _logger.LogInformation($"Output: {stdout}");

            // The 'query' command prints result to stdout.
            // The 'invoke' command prints status to stderr usually ("Chaincode invoke successful") 
            // and might imply success if exit code 0.
            
            if (method == "invoke")
            {
                return "Transaction Submitted";
            }

            return stdout.Trim();
        }
    }
}
