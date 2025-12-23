using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hyperledger.Fabric.Shim;
using Newtonsoft.Json;

namespace ClaimsContract
{
    public class ClaimsContract : IChaincode
    {
        public async Task<Response> Init(IChaincodeStub stub)
        {
            return Shim.Success();
        }

        public async Task<Response> Invoke(IChaincodeStub stub)
        {
            var function = stub.GetFunctionAndParameters().Function;
            var args = stub.GetFunctionAndParameters().Parameters;

            try
            {
                switch (function)
                {
                    case "InitLedger":
                        return await InitLedger(stub);
                    case "CreateClaim":
                        return await CreateClaim(stub, args);
                    case "ApproveClaim":
                        return await ApproveClaim(stub, args);
                    case "SettleClaim":
                        return await SettleClaim(stub, args);
                    case "ReadClaim":
                        return await ReadClaim(stub, args);
                    case "ClaimExists":
                        return await ClaimExists(stub, args);
                    case "GetAllClaims":
                        return await GetAllClaims(stub);
                    default:
                        return Shim.Error($"Unknown function: {function}");
                }
            }
            catch (Exception ex)
            {
                return Shim.Error(ex.ToString());
            }
        }

        private async Task<Response> InitLedger(IChaincodeStub stub)
        {
            var claims = new List<Claim>
            {
                new Claim { ID = "CLM101", PolicyID = "POLCA123", ClaimantName = "John Doe", Amount = 500.0, Description = "Windshield repair", Status = "SUBMITTED", SubmitterMSP = "Org1MSP", ApproverMSP = "Org2MSP", CreationDate = "2023-01-15T10:00:00Z", SettlementDate = "" },
                new Claim { ID = "CLM102", PolicyID = "POLCB456", ClaimantName = "Jane Smith", Amount = 1200.0, Description = "Bumper replacement", Status = "SETTLED", SubmitterMSP = "Org2MSP", ApproverMSP = "Org1MSP", CreationDate = "2023-01-20T14:30:00Z", SettlementDate = "2023-01-25T09:00:00Z" }
            };

            foreach (var claim in claims)
            {
                await stub.PutStateAsync(claim.ID, claim.ToString());
            }

            return Shim.Success();
        }

        private async Task<Response> CreateClaim(IChaincodeStub stub, IList<string> args)
        {
            if (args.Count != 6) return Shim.Error("Incorrect number of arguments. Expecting 6");

            var id = args[0];
            var policyID = args[1];
            var claimantName = args[2];
            if (!double.TryParse(args[3], out double amount)) return Shim.Error("Amount must be a number");
            var description = args[4];
            var approverMSP = args[5];

            var existsJson = await stub.GetStateAsync(id);
            if (!string.IsNullOrEmpty(existsJson))
            {
                return Shim.Error($"The claim {id} already exists");
            }

            // Using Creators or ClientIdentity requires more decoding, for simplicity we trust the arg or use basic stub if needed. 
            // In C# Shim, getting MSP ID usually needs parsing the creator byte array.
            // For now, let's keep it simple or implement a helper.
            // stub.GetCreator() returns byte[].

            var claim = new Claim
            {
                ID = id,
                PolicyID = policyID,
                ClaimantName = claimantName,
                Amount = amount,
                Description = description,
                Status = "SUBMITTED",
                SubmitterMSP = "DeterminedByCert", // Placeholder as GetCreator parsing is complex without helper
                ApproverMSP = approverMSP,
                CreationDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                SettlementDate = ""
            };

            await stub.PutStateAsync(id, claim.ToString());
            return Shim.Success();
        }

        private async Task<Response> ApproveClaim(IChaincodeStub stub, IList<string> args)
        {
            if (args.Count != 1) return Shim.Error("Incorrect number of arguments. Expecting 1");
            var id = args[0];

            var claimJson = await stub.GetStateAsync(id);
            if (string.IsNullOrEmpty(claimJson)) return Shim.Error($"Claim {id} does not exist");

            var claim = JsonConvert.DeserializeObject<Claim>(claimJson.ToStringUtf8());

            if (claim.Status != "SUBMITTED") return Shim.Error($"Claim {id} is not in SUBMITTED state");

            claim.Status = "APPROVED";
            await stub.PutStateAsync(id, claim.ToString());
            return Shim.Success();
        }

        private async Task<Response> SettleClaim(IChaincodeStub stub, IList<string> args)
        {
            if (args.Count != 1) return Shim.Error("Incorrect number of arguments. Expecting 1");
            var id = args[0];

            var claimJson = await stub.GetStateAsync(id);
            if (string.IsNullOrEmpty(claimJson)) return Shim.Error($"Claim {id} does not exist");

            var claim = JsonConvert.DeserializeObject<Claim>(claimJson.ToStringUtf8());

            if (claim.Status != "APPROVED") return Shim.Error($"Claim {id} must be APPROVED before settlement");

            claim.Status = "SETTLED";
            claim.SettlementDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            await stub.PutStateAsync(id, claim.ToString());
            return Shim.Success();
        }

        private async Task<Response> ReadClaim(IChaincodeStub stub, IList<string> args)
        {
            if (args.Count != 1) return Shim.Error("Incorrect number of arguments. Expecting 1");
            var id = args[0];

            var claimJson = await stub.GetStateAsync(id);
            if (string.IsNullOrEmpty(claimJson)) return Shim.Error($"Claim {id} does not exist");

            return Shim.Success(claimJson);
        }

        private async Task<Response> ClaimExists(IChaincodeStub stub, IList<string> args)
        {
            if (args.Count != 1) return Shim.Error("Incorrect number of arguments. Expecting 1");
            var id = args[0];

            var claimJson = await stub.GetStateAsync(id);
            var exists = !string.IsNullOrEmpty(claimJson);
            
            return Shim.Success(Encoding.UTF8.GetBytes(exists.ToString().ToLower()));
        }

        private async Task<Response> GetAllClaims(IChaincodeStub stub)
        {
            var iterator = await stub.GetStateByRangeAsync("", "");
            var claims = new List<Claim>();

            foreach (var result in iterator)
            {
                var claim = JsonConvert.DeserializeObject<Claim>(result.Value.ToStringUtf8());
                claims.Add(claim);
            }

            return Shim.Success(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(claims)));
        }
    }
}
