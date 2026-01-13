var dataTable;

$(document).ready(function () {
    var url = window.location.search;
    var status = "all";
    
    if (url.includes("inprocess")) {
        status = "inprocess";
    } else if (url.includes("completed")) {
        status = "completed";
    } else if (url.includes("pending")) {
        status = "pending";
    } else if (url.includes("approved")) {
        status = "approved";
    }
    
    loadDataTable(status);
    
    // Search functionality
    $('#orderSearch').on('keyup', function() {
        dataTable.search(this.value).draw();
    });
});

function loadDataTable(status) {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: '/admin/order/getall?status=' + status },
        "columns": [
            { 
                data: 'id', 
                "width": "8%",
                "render": function (data) {
                    return `<span class="fw-bold">#${data}</span>`;
                }
            },
            { 
                data: 'name', 
                "width": "20%" 
            },
            { 
                data: 'phoneNumber', 
                "width": "15%" 
            },
            { 
                data: 'applicationUser.email', 
                "width": "20%" 
            },
            { 
                data: 'orderStatus', 
                "width": "12%",
                "render": function (data) {
                    var badgeClass = "zen-badge--pending";
                    if (data === "Approved" || data === "Processing") {
                        badgeClass = "zen-badge--approved";
                    } else if (data === "Shipped" || data === "Completed") {
                        badgeClass = "zen-badge--completed";
                    } else if (data === "Cancelled" || data === "Refunded") {
                        badgeClass = "zen-badge--cancelled";
                    }
                    return `<span class="zen-badge ${badgeClass}">${data}</span>`;
                }
            },
            { 
                data: 'orderTotal', 
                "width": "10%",
                "render": function (data) {
                    return `<span class="fw-bold text-primary">$${data.toFixed(2)}</span>`;
                }
            },
            {
                data: 'id',
                "render": function (data) {
                    return `<div class="zen-table-actions">
                        <a href="/admin/order/details?orderId=${data}" class="zen-icon-btn" title="View Details">
                            <i class="bi bi-eye"></i>
                        </a>
                    </div>`;
                },
                "width": "15%"
            }
        ],
        "pageLength": 10,
        "order": [[0, "desc"]],
        "language": {
            "search": "",
            "searchPlaceholder": "Search orders...",
            "emptyTable": "No orders found"
        },
        "dom": '<"row"<"col-sm-12"tr>><"row"<"col-sm-6"i><"col-sm-6"p>>'
    });
}
