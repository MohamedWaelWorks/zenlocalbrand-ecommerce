var dataTable;
$(document).ready(function () {
    loadDataTable();
    
    // Search functionality
    $('#productSearch').on('keyup', function() {
        dataTable.search(this.value).draw();
    });
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: '/admin/product/getall'},
        "columns": [
            { 
                data: 'title', 
                "width": "30%",
                "render": function (data, type, row) {
                    var imgUrl = row.productImages && row.productImages.length > 0 
                        ? row.productImages[0].imageUrl 
                        : 'https://placehold.co/100x100/png';
                    return `<div class="zen-product-mini">
                        <img src="${imgUrl}" class="zen-product-mini__img" alt="${data}">
                        <div class="zen-product-mini__info">
                            <h4>${data}</h4>
                            <p>${row.category.name}</p>
                        </div>
                    </div>`;
                }
            },
            { data: 'isbn', "width": "15%" },
            { 
                data: 'listPrice', 
                "width": "10%",
                "render": function (data) {
                    return '$' + data.toFixed(2);
                }
            },
            { data: 'author', "width": "15%" },
            { 
                data: 'category.name', 
                "width": "15%",
                "render": function (data) {
                    return `<span class="zen-badge zen-badge--pending">${data}</span>`;
                }
            },
            {
                data: 'id',
                "render": function (data) {
                    return `<div class="zen-table-actions">
                        <a href="/admin/product/upsert?id=${data}" class="zen-icon-btn" title="Edit">
                            <i class="bi bi-pencil"></i>
                        </a>
                        <a OnClick=Delete('/admin/product/delete/${data}') class="zen-icon-btn zen-icon-btn--danger" title="Delete">
                            <i class="bi bi-trash"></i>
                        </a>
                    </div>`;
                },
                "width": "15%"
            }
        ],
        "pageLength": 10,
        "language": {
            "search": "",
            "searchPlaceholder": "Search...",
            "emptyTable": "No products found"
        },
        "dom": '<"row"<"col-sm-12"tr>><"row"<"col-sm-6"i><"col-sm-6"p>>'
    });
}

function Delete(url){
    Swal.fire({
        title: 'Delete Product?',
        text: "This action cannot be undone!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#ef4444',
        cancelButtonColor: '#6b7280',
        confirmButtonText: 'Yes, delete it!',
        cancelButtonText: 'Cancel',
        background: 'rgba(15, 18, 28, 0.95)',
        color: '#c3c7d4',
        iconColor: '#ffb547'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                type: 'DELETE',
                success: function (data) {
                    dataTable.ajax.reload();
                    toastr.success(data.message);
                }
            })
        }
    })
}
