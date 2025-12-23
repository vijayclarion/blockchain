const express = require('express');
const bodyParser = require('body-parser');
const cors = require('cors');
const { connectToNetwork } = require('./fabricHelpers');

const app = express();
app.use(cors());
app.use(bodyParser.json());

const PORT = 3000;
const USER_ID = 'appUser'; // In a real app, this comes from auth token

// GET all claims
app.get('/claims', async (req, res) => {
    try {
        const { contract, gateway } = await connectToNetwork(USER_ID);
        const result = await contract.evaluateTransaction('GetAllClaims');
        await gateway.disconnect();
        res.status(200).json(JSON.parse(result.toString()));
    } catch (error) {
        console.error(`Failed to evaluate transaction: ${error}`);
        res.status(500).json({ error: error.message });
    }
});

// GET claim by ID
app.get('/claims/:id', async (req, res) => {
    try {
        const { contract, gateway } = await connectToNetwork(USER_ID);
        const result = await contract.evaluateTransaction('ReadClaim', req.params.id);
        await gateway.disconnect();
        res.status(200).json(JSON.parse(result.toString()));
    } catch (error) {
        console.error(`Failed to evaluate transaction: ${error}`);
        res.status(500).json({ error: error.message });
    }
});

// POST Create Claim
app.post('/claims', async (req, res) => {
    try {
        // Expected body: { id, policyID, claimantName, amount, description, approverMSP }
        const { id, policyID, claimantName, amount, description, approverMSP } = req.body;

        const { contract, gateway } = await connectToNetwork(USER_ID);

        // Transaction: CreateClaim(id, policyID, claimantName, amount, description, approverMSP)
        // Note: Amount passed as string to submitTransaction if needed, or handle type conversion in Go. 
        // For Fabric Go ContractAPI: simple types usually work if passed as string arguments.
        await contract.submitTransaction('CreateClaim', id, policyID, claimantName, amount.toString(), description, approverMSP);

        await gateway.disconnect();
        res.status(201).json({ message: `Claim ${id} created` });
    } catch (error) {
        console.error(`Failed to submit transaction: ${error}`);
        res.status(500).json({ error: error.message });
    }
});

// POST Approve Claim
app.post('/claims/:id/approve', async (req, res) => {
    try {
        const { contract, gateway } = await connectToNetwork(USER_ID);
        await contract.submitTransaction('ApproveClaim', req.params.id);
        await gateway.disconnect();
        res.status(200).json({ message: `Claim ${req.params.id} approved` });
    } catch (error) {
        console.error(`Failed to submit transaction: ${error}`);
        res.status(500).json({ error: error.message });
    }
});

// POST Settle Claim
app.post('/claims/:id/settle', async (req, res) => {
    try {
        const { contract, gateway } = await connectToNetwork(USER_ID);
        await contract.submitTransaction('SettleClaim', req.params.id);
        await gateway.disconnect();
        res.status(200).json({ message: `Claim ${req.params.id} settled` });
    } catch (error) {
        console.error(`Failed to submit transaction: ${error}`);
        res.status(500).json({ error: error.message });
    }
});

app.listen(PORT, () => {
    console.log(`Claims dApp API running on port ${PORT}`);
});
