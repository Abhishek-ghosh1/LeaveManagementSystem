(function ($) {
    $.fn.universalValidate = function (options) {
        const settings = $.extend({
            scrollToError: true,
            liveValidation: true
        }, options);

        const validateForm = ($form) => {
            let isValid = true;
            let firstInvalid = null;

            $form.find(".input-error").removeClass("input-error is-invalid");
            $form.find(".error-message").remove();

            $form.find(".req").each(function () {
                let $el = $(this);
                if (!$el.is(":visible")) return;

                let tag = $el.prop("tagName").toLowerCase();
                let type = $el.attr("type");
                let val = ($el.val() ?? "").toString().trim();
                let isEmpty = false;
                let msg = "";

                if (tag === "select" || tag === "textarea") {
                    isEmpty = !val;
                } else if (type === "checkbox" || type === "radio") {
                    let group = $el.closest("[data-group]");
                    isEmpty = group.find(":checked").length === 0;
                    $el = group;
                } else if (type === "file") {
                    isEmpty = !$el[0].files.length;
                } else {
                    isEmpty = !val;
                }

                if (!isEmpty) {
                    let min = $el.attr("data-minlength");
                    let max = $el.attr("data-maxlength");
                    let pattern = $el.attr("pattern");

                    if (min && val.length < parseInt(min)) {
                        isEmpty = true;
                        msg = `Minimum ${min} characters required.`;
                    } else if (max && val.length > parseInt(max)) {
                        isEmpty = true;
                        msg = `Maximum ${max} characters allowed.`;
                    } else if (pattern && !(new RegExp(pattern).test(val))) {
                        isEmpty = true;
                        msg = `Invalid format.`;
                    } else if (type === "email" && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(val)) {
                        isEmpty = true;
                        msg = `Please enter a valid email.`;
                    }
                }

                if (isEmpty) {
                    isValid = false;
                    if (!firstInvalid) firstInvalid = $el;
                    msg = msg || $el.data("error") || "This field is required.";

                    $el.addClass("input-error is-invalid");

                    if ($el.next(".error-message").length === 0) {
                        $el.after(`<small class="error-message text-danger m-2">${msg}</small>`);
                    }
                }
            });

            if (!isValid && settings.scrollToError && firstInvalid) {
                $('html, body').animate({
                    scrollTop: firstInvalid.offset().top - 100
                }, 400);
            }

            return isValid;
        };

        // 🧠 Universal return logic
        if (this.length === 1 && this.is("form")) {
            return validateForm(this);
        }

        // 🧩 Setup submit handler and live validation
        return this.each(function () {
            const $form = $(this);

            $form.on("submit", function (e) {
                if (!validateForm($form)) {
                    e.preventDefault();
                }
            });

            if (settings.liveValidation) {
                $form.on("input blur change", ".req", function () {
                    validateForm($form);
                });
            }
        });
    };
})(jQuery);
