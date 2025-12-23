const { Gateway, Wallets } = require('fabric-network');
const path = require('path');
const fs = require('fs');

const channelName = 'mychannel';
const chaincodeName = 'basic'; // or 'claims'
const mspOrg1 = 'Org1MSP';
const walletPath = path.join(__dirname, 'wallet');

exports.connectToNetwork = async (userId) => {
    // load the network configuration
    const ccpPath = path.resolve(__dirname, '..', '..', '..', 'test-network', 'organizations', 'peerOrganizations', 'org1.example.com', 'connection-org1.json');
    
    // Check if network config exists (it might not if network isn't up)
    if (!fs.existsSync(ccpPath)) {
        throw new Error(`CCP Path not found: ${ccpPath}. Make sure the test-network is up.`);
    }
    
    const ccp = JSON.parse(fs.readFileSync(ccpPath, 'utf8'));

    // Create a new file system based wallet for managing identities.
    const wallet = await Wallets.newFileSystemWallet(walletPath);
    
    // Check to see if we've already enrolled the user.
    const identity = await wallet.get(userId);
    if (!identity) {
        throw new Error(`An identity for the user "${userId}" does not exist in the wallet. Enroll the admin/user first.`);
    }

    // Create a new gateway for connecting to our peer node.
    const gateway = new Gateway();
    await gateway.connect(ccp, { wallet, identity: userId, discovery: { enabled: true, asLocalhost: true } });

    // Get the network (channel) our contract is deployed to.
    const network = await gateway.getNetwork(channelName);

    // Get the contract from the network.
    const contract = network.getContract(chaincodeName);

    return { contract, gateway };
};
