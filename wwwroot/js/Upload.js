/**
 * Sets up file preview functionality for file inputs
 * @param {string} inputSelector - Selector for the file input element
 * @param {string} previewSelector - Selector for the preview container
 * @param {string} errorSelector - Selector for the error message container
 * @param {number} [maxFiles=2] - Maximum number of allowed files
 * @param {number} [maxFileSizeMB=5] - Maximum file size in megabytes
 */
function setupFilePreview(inputSelector, previewSelector, errorSelector, maxFiles = 2, maxFileSizeMB = 5) {
    const $input = $(inputSelector);
    const $previewContainer = $(previewSelector);
    const $errorContainer = $(errorSelector);

    let selectedFiles = []; // store files

    // File type icons mapping
    const fileIcons = {
        'pdf': { icon: 'fa-file-pdf', color: '#dc3545' },
        'doc': { icon: 'fa-file-word', color: '#2b579a' },
        'docx': { icon: 'fa-file-word', color: '#2b579a' },
        'xls': { icon: 'fa-file-excel', color: '#217346' },
        'xlsx': { icon: 'fa-file-excel', color: '#217346' },
        'ppt': { icon: 'fa-file-powerpoint', color: '#b7472a' },
        'pptx': { icon: 'fa-file-powerpoint', color: '#b7472a' },
        'txt': { icon: 'fa-file-alt', color: '#6c757d' },
        'csv': { icon: 'fa-file-csv', color: '#28a745' },
        'zip': { icon: 'fa-file-archive', color: '#ffc107' },
        'rar': { icon: 'fa-file-archive', color: '#ffc107' },
        '7z': { icon: 'fa-file-archive', color: '#ffc107' },
        'jpg': { icon: 'fa-file-image', color: '#17a2b8' },
        'jpeg': { icon: 'fa-file-image', color: '#17a2b8' },
        'png': { icon: 'fa-file-image', color: '#17a2b8' },
        'gif': { icon: 'fa-file-image', color: '#17a2b8' },
        'svg': { icon: 'fa-file-image', color: '#17a2b8' },
        'webp': { icon: 'fa-file-image', color: '#17a2b8' },
        'mp4': { icon: 'fa-file-video', color: '#6f42c1' },
        'avi': { icon: 'fa-file-video', color: '#6f42c1' },
        'mov': { icon: 'fa-file-video', color: '#6f42c1' },
        'mp3': { icon: 'fa-file-audio', color: '#e83e8c' },
        'wav': { icon: 'fa-file-audio', color: '#e83e8c' },
        'json': { icon: 'fa-file-code', color: '#fd7e14' },
        'js': { icon: 'fa-file-code', color: '#fd7e14' },
        'html': { icon: 'fa-file-code', color: '#fd7e14' },
        'css': { icon: 'fa-file-code', color: '#fd7e14' },
        'default': { icon: 'fa-file', color: '#6c757d' }
    };

    // When input changes
    $input.on('change', function (e) {
        const files = Array.from(e.target.files);
        $previewContainer.empty();
        $errorContainer.text('');
        selectedFiles = []; // reset

        // Validate file count
        if (files.length > maxFiles) {
            showError(`You can only upload a maximum of ${maxFiles} file(s).`);
            $previewContainer.empty();
            return;
        }

        // Process each file
        files.forEach((file, idx) => {
            if (!validateFile(file)) return;

            selectedFiles.push(file);

            if (file.type.startsWith('image/')) {
                const reader = new FileReader();
                reader.onload = createImagePreviewHandler(file.name, idx);
                reader.readAsDataURL(file);
            } else {
                createDocumentPreview(file, idx);
            }
        });

        syncFiles(); // sync to input
    });

    /**
     * Validates a single file
     */
    function validateFile(file) {
        if (file.size > maxFileSizeMB * 1024 * 1024) {
            showError(`File size must be less than ${maxFileSizeMB}MB.`);

            return false;
        }
        return true;
    }

    /**
     * Creates preview handler for images
     */
    function createImagePreviewHandler(fileName, index) {
        return function (event) {
            const $wrapper = createPreviewWrapper({ name: fileName, size: 0 }, index);
            const $img = $('<img>')
                .attr({
                    'src': event.target.result,
                    'alt': `Preview of ${fileName}`,
                    'loading': 'lazy'
                })
                .addClass('img-thumbnail')
                .css({
                    'max-width': '120px',
                    'max-height': '120px',
                    'object-fit': 'cover',
                    'border-radius': '0.375rem'
                })
                .on('error', function () {
                    $(this).parent().remove();
                });
            $wrapper.append($img);
            $previewContainer.append($wrapper);
        };
    }


    /**
     * Creates preview for documents
     */

    function getFileExtension(fileName) {
        return fileName.split('.').pop() || '';
    }

    function formatFileSize(bytes) {
        if (bytes === 0) return '0 B';
        const k = 1024;
        const sizes = ['B', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`;
    }
    function createDocumentPreview(file, index) {
        const extension = file.name.split('.').pop().toLowerCase();
        const iconClass = fileIcons[extension] || fileIcons['default'];

        const $wrapper = createPreviewWrapper(file.name, index);

        const $iconContainer = $('<div>')
            .addClass('file-preview-container')
            .css({
                'width': '120px',
                'height': '120px',
                'display': 'flex',
                'flex-direction': 'column',
                'align-items': 'center',
                'justify-content': 'center',
                'background': 'linear-gradient(145deg, #f8f9fa, #e9ecef)',
                'border': '2px dashed #dee2e6',
                'border-radius': '0.5rem',
                'transition': 'all 0.2s ease',
                'cursor': 'pointer'
            })
            .hover(
                function () { $(this).css('border-color', '#007bff'); },
                function () { $(this).css('border-color', '#dee2e6'); }
            );

        const $icon = $('<i>')
            .addClass(`fas ${iconClass.icon}`)
            .css({
                'font-size': '2.5rem',
                'color': iconClass.color,
                'margin-bottom': '0.5rem'
            });


        const $fileName = $('<div>')
            .text(truncateFileName(file.name))
            .addClass('file-name-preview')
            .css({
                'max-width': '110px',
                'overflow': 'hidden',
                'text-overflow': 'ellipsis',
                'white-space': 'nowrap',
                'text-align': 'center',
                'font-size': '0.75rem',
                'color': '#495057',
                'font-weight': '500'
            });

        const $fileSize = $('<div>')
            .text(formatFileSize(file.size))
            .css({
                'font-size': '0.65rem',
                'color': '#6c757d',
                'margin-top': '0.25rem'
            });

        $iconContainer.append($icon, $fileName, $fileSize);
        $wrapper.append($iconContainer);
        $previewContainer.append($wrapper);
    }

    /**
     * Creates preview wrapper with delete button
     */

    function createPreviewWrapper(file, index) {
        const $wrapper = $('<div>')
            .addClass('file-preview-wrapper position-relative d-inline-block m-2')
            .attr({
                'data-index': index,
                'data-filename': file.name,
                'title': `${file.name} (${formatFileSize(file.size)})`
            })
            .css({
                'transition': 'transform 0.2s ease',
                'animation': 'fadeInScale 0.3s ease'
            })
            .hover(
                function () { $(this).css('transform', 'scale(1.05)'); },
                function () { $(this).css('transform', 'scale(1)'); }
            );

        const $deleteBtn = $('<button>')
            .attr({
                'type': 'button',
                'aria-label': `Remove ${file.name}`,
                'title': 'Remove file'
            })
            .addClass('btn btn-danger btn-sm position-absolute file-delete-btn')
            .css({
                'top': '-8px',
                'right': '-8px',
                'width': '24px',
                'height': '24px',
                'padding': '0',
                'border-radius': '50%',
                'display': 'flex',
                'align-items': 'center',
                'justify-content': 'center',
                'box-shadow': '0 2px 4px rgba(0,0,0,0.2)',
                'z-index': '10'
            })
            .html('<i class="fas fa-times" style="font-size: 0.75rem;"></i>')
            .on('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                removeFile(index, $wrapper);
            });

        return $wrapper.append($deleteBtn);
    }

    /**
     * Remove file with animation
     */
    function removeFile(index, $wrapper) {
        const removedFile = selectedFiles[index];

        // Animate removal
        $wrapper.css({
            'transform': 'scale(0)',
            'opacity': '0',
            'transition': 'all 0.2s ease'
        });

        setTimeout(() => {
            selectedFiles.splice(index, 1);
            $wrapper.remove();
            syncFiles();
            updateIndices();
        }, 200);
    }

    /**
     * Update data-index attributes after file removal
     */
    function updateIndices() {
        $previewContainer.find('.file-preview-wrapper').each(function (newIndex) {
            $(this).attr('data-index', newIndex);
        });
    }

    /**
     * Truncate file name
     */
    function truncateFileName(fileName) {
        const maxLength = 15;
        const extension = fileName.split('.').pop();
        const nameWithoutExt = fileName.substring(0, fileName.length - extension.length - 1);

        if (fileName.length <= maxLength) return fileName;

        return nameWithoutExt.substring(0, maxLength - extension.length - 3) +
            '...' +
            extension;
    }

    /**
     * Show error
     */

    function showError(message) {
        $errorContainer.html(message.replace(/\n/g, '<br>'))
            .addClass('alert alert-danger')
            .css('animation', 'shake 0.5s ease');
        resetInput();
    }

    $input.on('click focus', function () {
        resetInput();
        clearErrors();
    });
    function clearErrors() {
        $errorContainer.empty().removeClass('alert alert-danger');
    }

    function resetInput() {
        $input.val('');
        selectedFiles = [];
        $previewContainer.html();
        $previewContainer.empty();
    }
    /**
     * Sync selectedFiles back into input.files
     */
    function syncFiles() {
        try {
            const dt = new DataTransfer();
            selectedFiles.forEach(file => dt.items.add(file));
            $input[0].files = dt.files;
        } catch (error) {
            console.warn('Could not sync files to input:', error);
        }
    }


}
