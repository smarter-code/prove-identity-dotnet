class ProveIdentityApp {
    constructor() {
        this.apiService = new ApiService();
        this.uiManager = new UIManager();
        this.correlationId = null;
        this.authToken = null;

        this.init();
    }

    init() {
        this.bindEvents();
        this.setupFormValidation();

        // Check if there's a stored correlation ID (for page refresh scenarios)
        this.correlationId = sessionStorage.getItem('correlationId');
    }

    bindEvents() {
        // Phone form submission
        const phoneForm = document.getElementById('phone-form');
        phoneForm.addEventListener('submit', (e) => this.handlePhoneSubmission(e));

        // Personal info form submission
        const personalInfoForm = document.getElementById('personal-info-form');
        personalInfoForm.addEventListener('submit', (e) => this.handlePersonalInfoSubmission(e));

        // Phone number formatting
        const phoneInput = document.getElementById('phone');
        phoneInput.addEventListener('input', () => this.uiManager.formatPhoneNumber(phoneInput));

        // SSN input restrictions
        const ssnInputs = document.querySelectorAll('#ssn, #fullSSN');
        ssnInputs.forEach(input => {
            input.addEventListener('input', (e) => {
                e.target.value = e.target.value.replace(/\D/g, '');
            });
        });
    }

    setupFormValidation() {
        // Real-time validation for inputs
        document.querySelectorAll('input').forEach(input => {
            input.addEventListener('blur', () => {
                if (input.hasAttribute('required') && !input.value.trim()) {
                    this.uiManager.showFieldError(input, 'This field is required');
                } else {
                    this.uiManager.clearFieldError(input);
                }
            });

            input.addEventListener('input', () => {
                this.uiManager.clearFieldError(input);
            });
        });
    }

    async handlePhoneSubmission(e) {
        e.preventDefault();

        const formData = new FormData(e.target);
        const phoneNumber = formData.get('phone').replace(/\D/g, '');
        const lastFourSSN = formData.get('ssn');

        if (!this.uiManager.validateForm(e.target)) {
            return;
        }

        this.uiManager.showLoading(true);

        try {
            // Determine flow type based on device
            const flowType = this.isMobileDevice() ? 'mobile' : 'desktop';

            // start verification
            const response = await this.apiService.initiateVerification(
                phoneNumber,
                lastFourSSN,
                flowType
            );

            if (response.success) {
                this.correlationId = response.data.correlationId;
                this.authToken = response.data.authToken;

                // Store correlation ID for persistence
                sessionStorage.setItem('correlationId', this.correlationId);
                this.uiManager.showLoading(false);
                // Move to authentication step
                this.uiManager.showStep(2);
                this.uiManager.showNotification('Verification initiated successfully', 'success');
                setTimeout(() => {
                    console.log("Simulate user clicking on the link in the mobile");
                }, 5000);
                // Start authentication process
                await this.startAuthentication();
            } else {
                throw new Error(response.message || 'Failed to initiate verification');
            }
        } catch (error) {
            console.error('Verification initiation failed:', error);
            this.uiManager.showNotification(
                error.message || 'Failed to start verification. Please try again.',
                'error'
            );
        } finally {
          
        }
    }

    async startAuthentication() {
        try {
            // Check if Prove Auth SDK is available
            if (typeof proveAuth === 'undefined') {
                throw new Error('Prove Auth SDK not loaded');
            }

            // Configure authentication based on device type
            let builder = new proveAuth.AuthenticatorBuilder();

            if (this.isMobileDevice()) {
                builder = builder
                    .withAuthFinishStep((input) => this.handleAuthFinish(input.authId))
                    .withOtpFallback(
                        (phoneNumberNeeded, phoneValidationError) => this.handleOtpStart(phoneNumberNeeded, phoneValidationError),
                        (otpCode) => this.handleOtpFinish(otpCode)
                    );
            } else {
                builder = builder
                    .withAuthFinishStep((input) => this.handleAuthFinish(input.authId))
                    .withInstantLinkFallback((phoneNumberNeeded, phoneValidationError) => this.handleInstantLink(phoneNumberNeeded, phoneValidationError))
                    .withRole("secondary");
            }

            const authenticator = builder.build();

            // Start authentication
            await authenticator.authenticate(this.authToken);

        } catch (error) {
            console.error('Authentication error:', error);
            this.uiManager.showNotification('Authentication failed. Please try again.', 'error');
            this.uiManager.showStep(1); // Go back to step 1
        }
    }

    async handleAuthFinish(authId) {
        try {
            // Validate the phone number
            const response = await this.apiService.validatePhone(this.correlationId);

            if (response.success) {
                // Move to personal info step
                this.uiManager.showStep(3);
                this.uiManager.showNotification('Phone verification successful', 'success');
            } else {
                throw new Error(response.message || 'Phone validation failed');
            }
        } catch (error) {
            console.error('Phone validation error:', error);
            this.uiManager.showNotification(
                error.message || 'Phone validation failed. Please try again.',
                'error'
            );
        }
    }

    handleOtpStart(phoneNumberNeeded, phoneValidationError) {
        return new Promise((resolve, reject) => {
            if (phoneNumberNeeded) {
                const phoneNumber = prompt("Please enter your phone number:");
                if (phoneNumber) {
                    resolve({ phoneNumber: phoneNumber.replace(/\D/g, '') });
                } else {
                    reject(new Error('Phone number required'));
                }
            } else {
                resolve(null);
            }
        });
    }

    handleOtpFinish(otpCode) {
        return new Promise((resolve, reject) => {
            const code = prompt("Please enter the OTP code sent to your phone:");
            if (code) {
                resolve({ otpCode: code });
            } else {
                reject(new Error('OTP code required'));
            }
        });
    }

    handleInstantLink(phoneNumberNeeded, phoneValidationError) {
        return new Promise((resolve, reject) => {
            if (phoneNumberNeeded) {
                const phoneNumber = prompt("Please enter your phone number:");
                if (phoneNumber) {
                    resolve({ phoneNumber: phoneNumber.replace(/\D/g, '') });
                } else {
                    reject(new Error('Phone number required'));
                }
            } else {
                resolve(null);
            }
        });
    }

    async handlePersonalInfoSubmission(e) {
        e.preventDefault();

        if (!this.uiManager.validateForm(e.target)) {
            return;
        }

        const formData = new FormData(e.target);
        const individual = {
            first_name: formData.get('firstName'),
            last_name: formData.get('lastName'),
            email_addresses: [formData.get('email')],
            addresses: [{address: formData.get('address'), city: formData.get('city'),post_code: formData.get('post_code')}],
            dob: formData.get('dob'),
            ssn: formData.get('fullSSN')
        };

        this.uiManager.showLoading(true);

        try {
            const response = await this.apiService.completeVerification(
                this.correlationId,
                individual
            );

            if (response.success) {
                this.uiManager.showSuccess();
                // Clear stored correlation ID
                sessionStorage.removeItem('correlationId');
            } else {
                throw new Error(response.message || 'Failed to complete verification');
            }
        } catch (error) {
            console.error('Verification completion failed:', error);
            this.uiManager.showNotification(
                error.message || 'Failed to complete verification. Please try again.',
                'error'
            );
        } finally {
            this.uiManager.showLoading(false);
        }
    }

    isMobileDevice() {
        return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
    }

    reset() {
        this.correlationId = null;
        this.authToken = null;
        sessionStorage.removeItem('correlationId');
        this.uiManager.reset();
    }
}

// Global function for reset button
function resetForm() {
    if (window.app) {
        window.app.reset();
    }
}

// Initialize the application when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.app = new ProveIdentityApp();
});