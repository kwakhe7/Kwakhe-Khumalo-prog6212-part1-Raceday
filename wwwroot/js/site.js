// RaceDay site scripts

// Friendly size check for image uploads (add data-max-mb and data-error to a file input).
// The API still makes the final decision about what it accepts.
document.addEventListener('change', function (event) {
    var input = event.target;
    if (!input.matches || !input.matches('input[type=file][data-max-mb]')) {
        return;
    }

    var error = input.dataset.error ? document.querySelector(input.dataset.error) : null;
    var maxBytes = parseFloat(input.dataset.maxMb) * 1024 * 1024;

    if (error) {
        error.textContent = '';
    }

    if (input.files && input.files.length > 0 && input.files[0].size > maxBytes) {
        if (error) {
            error.textContent = 'That image is larger than ' + input.dataset.maxMb + ' MB. Please choose a smaller one.';
        }
        input.value = '';
    }
});