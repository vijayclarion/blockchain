using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ClaimsContract
{
    public class Claim
    {
        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("policyID")]
        public string PolicyID { get; set; }

        [JsonProperty("claimantName")]
        public string ClaimantName { get; set; }

        [JsonProperty("amount")]
        public double Amount { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; } // SUBMITTED, APPROVED, REJECTED, SETTLED

        [JsonProperty("submitterMSP")]
        public string SubmitterMSP { get; set; }

        [JsonProperty("approverMSP")]
        public string ApproverMSP { get; set; }

        [JsonProperty("creationDate")]
        public string CreationDate { get; set; }

        [JsonProperty("settlementDate")]
        public string SettlementDate { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
