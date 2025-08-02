class ApiService {
    constructor() {
        this.baseUrl = '/api/verification';
    }

    async makeRequest(endpoint, options = {}) {
        const url = `${this.baseUrl}${endpoint}`;
        const defaultOptions = {
            headers: {
                'Content-Type': 'application/json',
            },
        };

        const finalOptions = { ...defaultOptions, ...options };

        try {
            const response = await fetch(url, finalOptions);
            const data = await response.json();

            if (!response.ok) {
                throw new Error(data.message || 'An error occurred');
            }

            return data;
        } catch (error) {
            console.error('API Error:', error);
            throw error;
        }
    }

    async initiateVerification(phoneNumber, lastFourSSN, flowType) {
        return this.makeRequest('/start', {
            method: 'POST',
            body: JSON.stringify({
                phoneNumber,
                lastFourSSN,
                flowType
            })
        });
    }

    async validatePhone(correlationId) {
        return this.makeRequest('/validate', {
            method: 'POST',
            body: JSON.stringify({
                correlationId
            })
        });
    }

    async completeVerification(correlationId, individual) {
        return this.makeRequest('/complete', {
            method: 'POST',
            body: JSON.stringify({
                correlationId,
                individual
            })
        });
    }
}

// Export for use in other scripts
window.ApiService = ApiService;