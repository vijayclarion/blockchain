using System.Text.Json.Serialization;

namespace ClaimsApi.Models
{
    public class Claim
    {
        [JsonPropertyName("id")]
        public string ID { get; set; } = string.Empty;

        [JsonPropertyName("policyID")]
        public string PolicyID { get; set; } = string.Empty;

        [JsonPropertyName("claimantName")]
        public string ClaimantName { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public double Amount { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("submitterMSP")]
        public string SubmitterMSP { get; set; } = string.Empty;

        [JsonPropertyName("approverMSP")]
        public string ApproverMSP { get; set; } = string.Empty;

        [JsonPropertyName("creationDate")]
        public string CreationDate { get; set; } = string.Empty;

        [JsonPropertyName("settlementDate")]
        public string SettlementDate { get; set; } = string.Empty;
    }

    public class CreateClaimDto
    {
        public string ID { get; set; }
        public string PolicyID { get; set; }
        public string ClaimantName { get; set; }
        public double Amount { get; set; }
        public string Description { get; set; }
        public string ApproverMSP { get; set; }
    }
}
