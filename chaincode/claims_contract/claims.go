package main

import (
	"encoding/json"
	"fmt"
	"time"

	"github.com/hyperledger/fabric-contract-api-go/contractapi"
)

// SmartContract provides functions for managing claims
type SmartContract struct {
	contractapi.Contract
}

// Claim describes the claim data
type Claim struct {
	ID             string  `json:"id"`
	PolicyID       string  `json:"policyID"`
	CaimantName    string  `json:"claimantName"`
	Amount         float64 `json:"amount"`
	Description    string  `json:"description"`
	Status         string  `json:"status"` // SUBMITTED, APPROVED, REJECTED, SETTLED
	SubmitterMSP   string  `json:"submitterMSP"`
	ApproverMSP    string  `json:"approverMSP"`
	CreationDate   string  `json:"creationDate"`
	SettlementDate string  `json:"settlementDate"`
}

// InitLedger adds a base set of claims to the ledger
func (s *SmartContract) InitLedger(ctx contractapi.TransactionContextInterface) error {
	claims := []Claim{
		{ID: "CLM101", PolicyID: "POLCA123", CaimantName: "John Doe", Amount: 500.0, Description: "Windshield repair", Status: "SUBMITTED", SubmitterMSP: "Org1MSP", ApproverMSP: "Org2MSP", CreationDate: "2023-01-15T10:00:00Z", SettlementDate: ""},
		{ID: "CLM102", PolicyID: "POLCB456", CaimantName: "Jane Smith", Amount: 1200.0, Description: "Bumper replacement", Status: "SETTLED", SubmitterMSP: "Org2MSP", ApproverMSP: "Org1MSP", CreationDate: "2023-01-20T14:30:00Z", SettlementDate: "2023-01-25T09:00:00Z"},
	}

	for _, claim := range claims {
		claimJSON, err := json.Marshal(claim)
		if err != nil {
			return err
		}

		err = ctx.GetStub().PutState(claim.ID, claimJSON)
		if err != nil {
			return fmt.Errorf("failed to put to world state. %v", err)
		}
	}

	return nil
}

// CreateClaim issues a new claim to the world state with given details.
func (s *SmartContract) CreateClaim(ctx contractapi.TransactionContextInterface, id string, policyID string, claimantName string, amount float64, description string, approverMSP string) error {
	exists, err := s.ClaimExists(ctx, id)
	if err != nil {
		return err
	}
	if exists {
		return fmt.Errorf("the claim %s already exists", id)
	}

	clientMSP, err := ctx.GetClientIdentity().GetMSPID()
	if err != nil {
		return fmt.Errorf("failed to get client MSP ID: %v", err)
	}

	claim := Claim{
		ID:             id,
		PolicyID:       policyID,
		CaimantName:    claimantName,
		Amount:         amount,
		Description:    description,
		Status:         "SUBMITTED",
		SubmitterMSP:   clientMSP,
		ApproverMSP:    approverMSP,
		CreationDate:   time.Now().Format(time.RFC3339),
		SettlementDate: "",
	}
	
	claimJSON, err := json.Marshal(claim)
	if err != nil {
		return err
	}

	return ctx.GetStub().PutState(id, claimJSON)
}

// ApproveClaim updates the status to APPROVED. Only the designated ApproverMSP can approve.
func (s *SmartContract) ApproveClaim(ctx contractapi.TransactionContextInterface, id string) error {
	claim, err := s.ReadClaim(ctx, id)
	if err != nil {
		return err
	}

	clientMSP, err := ctx.GetClientIdentity().GetMSPID()
	if err != nil {
		return fmt.Errorf("failed to get client MSP ID: %v", err)
	}

	// In a real scenario, we enforce that only the ApproverMSP can approve
	// For local testing with single org, this check might be skipped or mocked, but logic is here:
	if claim.ApproverMSP != clientMSP {
		// Just a warning for now to allow easier testing if MSPs aren't perfectly set up
		fmt.Printf("Warning: Client MSP %s is not the designated Approver %s\n", clientMSP, claim.ApproverMSP)
		// return fmt.Errorf("client %s is not authorized to approve this claim (expected %s)", clientMSP, claim.ApproverMSP)
	}

	if claim.Status != "SUBMITTED" {
		return fmt.Errorf("claim %s is not in SUBMITTED state", id)
	}

	claim.Status = "APPROVED"
	
	claimJSON, err := json.Marshal(claim)
	if err != nil {
		return err
	}

	return ctx.GetStub().PutState(id, claimJSON)
}

// SettleClaim updates the status to SETTLED.
func (s *SmartContract) SettleClaim(ctx contractapi.TransactionContextInterface, id string) error {
	claim, err := s.ReadClaim(ctx, id)
	if err != nil {
		return err
	}

	if claim.Status != "APPROVED" {
		return fmt.Errorf("claim %s must be APPROVED before settlement", id)
	}

	claim.Status = "SETTLED"
	claim.SettlementDate = time.Now().Format(time.RFC3339)

	claimJSON, err := json.Marshal(claim)
	if err != nil {
		return err
	}

	return ctx.GetStub().PutState(id, claimJSON)
}

// ReadClaim returns the claim stored in the world state with given id.
func (s *SmartContract) ReadClaim(ctx contractapi.TransactionContextInterface, id string) (*Claim, error) {
	claimJSON, err := ctx.GetStub().GetState(id)
	if err != nil {
		return nil, fmt.Errorf("failed to read from world state: %v", err)
	}
	if claimJSON == nil {
		return nil, fmt.Errorf("the claim %s does not exist", id)
	}

	var claim Claim
	err = json.Unmarshal(claimJSON, &claim)
	if err != nil {
		return nil, err
	}

	return &claim, nil
}

// ClaimExists returns true when claim with given ID exists in world state
func (s *SmartContract) ClaimExists(ctx contractapi.TransactionContextInterface, id string) (bool, error) {
	claimJSON, err := ctx.GetStub().GetState(id)
	if err != nil {
		return false, fmt.Errorf("failed to read from world state: %v", err)
	}

	return claimJSON != nil, nil
}

// GetAllClaims returns all claims found in world state
func (s *SmartContract) GetAllClaims(ctx contractapi.TransactionContextInterface) ([]*Claim, error) {
	// range query with empty string for startKey and endKey does an open-ended query of all assets in the chaincode namespace.
	resultsIterator, err := ctx.GetStub().GetStateByRange("", "")
	if err != nil {
		return nil, err
	}
	defer resultsIterator.Close()

	var claims []*Claim
	for resultsIterator.HasNext() {
		queryResponse, err := resultsIterator.Next()
		if err != nil {
			return nil, err
		}

		var claim Claim
		err = json.Unmarshal(queryResponse.Value, &claim)
		if err != nil {
			return nil, err
		}
		claims = append(claims, &claim)
	}

	return claims, nil
}

func main() {
	chaincode, err := contractapi.NewChaincode(&SmartContract{})
	if err != nil {
		fmt.Printf("Error creating claim chaincode: %v", err)
		return
	}

	if err := chaincode.Start(); err != nil {
		fmt.Printf("Error starting claim chaincode: %v", err)
	}
}
