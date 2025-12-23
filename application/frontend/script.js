const API_URL = 'http://localhost:3000';

document.addEventListener('DOMContentLoaded', () => {
    fetchClaims();
    setupModal();
});

async function fetchClaims() {
    try {
        const response = await fetch(`${API_URL}/claims`);
        const claims = await response.json();
        renderClaims(claims);
        updateStats(claims);
    } catch (error) {
        console.error('Error fetching claims:', error);
    }
}

function renderClaims(claims) {
    const tbody = document.getElementById('claims-table-body');
    tbody.innerHTML = '';

    claims.forEach(claim => {
        const tr = document.createElement('tr');
        const statusClass = claim.status.toLowerCase();

        // Actions Button
        let actionsHtml = '';
        if (claim.status === 'SUBMITTED') {
            actionsHtml = `<button class="btn btn-small primary" onclick="approveClaim('${claim.id}')">Approve</button>`;
        } else if (claim.status === 'APPROVED') {
            actionsHtml = `<button class="btn btn-small success-btn" style="background-color: var(--success-color); color: white;" onclick="settleClaim('${claim.id}')">Settle</button>`;
        }

        tr.innerHTML = `
            <td>#${claim.id}</td>
            <td>${claim.policyID}</td>
            <td>${claim.claimantName}</td>
            <td>$${claim.amount}</td>
            <td><span class="status ${statusClass}">${claim.status}</span></td>
            <td>${new Date(claim.creationDate).toLocaleDateString()}</td>
            <td>${actionsHtml}</td>
        `;
        tbody.appendChild(tr);
    });
}

function updateStats(claims) {
    document.getElementById('total-claims').textContent = claims.length;
    document.getElementById('pending-claims').textContent = claims.filter(c => c.status === 'SUBMITTED' || c.status === 'APPROVED').length;
    document.getElementById('settled-claims').textContent = claims.filter(c => c.status === 'SETTLED').length;
}

async function approveClaim(id) {
    if (!confirm(`Approve claim ${id}?`)) return;
    try {
        await fetch(`${API_URL}/claims/${id}/approve`, { method: 'POST' });
        fetchClaims();
    } catch (e) { console.error(e); }
}

async function settleClaim(id) {
    if (!confirm(`Settle claim ${id}?`)) return;
    try {
        await fetch(`${API_URL}/claims/${id}/settle`, { method: 'POST' });
        fetchClaims();
    } catch (e) { console.error(e); }
}

function setupModal() {
    const modal = document.getElementById('claim-modal');
    const btn = document.getElementById('new-claim-btn');
    const close = document.getElementById('close-modal');
    const form = document.getElementById('claim-form');

    btn.onclick = () => modal.classList.remove('hidden');
    close.onclick = () => modal.classList.add('hidden');
    window.onclick = (event) => {
        if (event.target == modal) modal.classList.add('hidden');
    };

    form.onsubmit = async (e) => {
        e.preventDefault();
        const data = {
            id: document.getElementById('claim-id').value,
            policyID: document.getElementById('policy-id').value,
            claimantName: document.getElementById('claimant-name').value,
            amount: document.getElementById('amount').value,
            description: document.getElementById('description').value,
            approverMSP: document.getElementById('approver-msp').value,
        };

        try {
            await fetch(`${API_URL}/claims`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            });
            modal.classList.add('hidden');
            form.reset();
            fetchClaims();
        } catch (error) {
            console.error('Error creating claim:', error);
        }
    };
}
