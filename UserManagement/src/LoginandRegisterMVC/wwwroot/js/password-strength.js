// Password strength indicator
(function() {
    'use strict';

    function checkPasswordStrength(password) {
        let strength = 0;
        let feedback = [];

        // Check length
        if (password.length >= 8) {
            strength += 20;
        } else {
            feedback.push('At least 8 characters');
        }

        // Check for lowercase
        if (/[a-z]/.test(password)) {
            strength += 20;
        } else {
            feedback.push('One lowercase letter');
        }

        // Check for uppercase
        if (/[A-Z]/.test(password)) {
            strength += 20;
        } else {
            feedback.push('One uppercase letter');
        }

        // Check for numbers
        if (/\d/.test(password)) {
            strength += 20;
        } else {
            feedback.push('One number');
        }

        // Check for special characters
        if (/[@$!%*?&#^()_+=\[\]{};':"\\|,.<>/?~`-]/.test(password)) {
            strength += 20;
        } else {
            feedback.push('One special character');
        }

        return {
            strength: strength,
            feedback: feedback
        };
    }

    function updatePasswordStrength(passwordInput, strengthMeter, feedbackElement) {
        const password = passwordInput.val();
        const result = checkPasswordStrength(password);

        // Update strength meter
        strengthMeter.removeClass('bg-danger bg-warning bg-info bg-success');
        strengthMeter.css('width', result.strength + '%');
        strengthMeter.attr('aria-valuenow', result.strength);

        // Update color based on strength
        if (result.strength < 40) {
            strengthMeter.addClass('bg-danger');
        } else if (result.strength < 60) {
            strengthMeter.addClass('bg-warning');
        } else if (result.strength < 80) {
            strengthMeter.addClass('bg-info');
        } else {
            strengthMeter.addClass('bg-success');
        }

        // Update feedback text
        if (result.feedback.length === 0) {
            feedbackElement.html('<span class="text-success"><i class="fas fa-check-circle"></i> Strong password!</span>');
        } else {
            feedbackElement.html('<span class="text-muted">Missing: ' + result.feedback.join(', ') + '</span>');
        }
    }

    // Initialize password strength checker on document ready
    $(document).ready(function() {
        const passwordInput = $('#Password');

        if (passwordInput.length > 0) {
            // Add strength meter HTML after password input
            const strengthHTML = `
                <div class="password-strength-container mt-2">
                    <div class="progress" style="height: 5px;">
                        <div class="progress-bar" role="progressbar" style="width: 0%"
                             aria-valuenow="0" aria-valuemin="0" aria-valuemax="100" id="password-strength-meter"></div>
                    </div>
                    <small id="password-strength-feedback" class="form-text"></small>
                </div>
            `;

            passwordInput.parent().append(strengthHTML);

            const strengthMeter = $('#password-strength-meter');
            const feedbackElement = $('#password-strength-feedback');

            // Update strength on input
            passwordInput.on('input', function() {
                updatePasswordStrength(passwordInput, strengthMeter, feedbackElement);
            });
        }
    });
})();
