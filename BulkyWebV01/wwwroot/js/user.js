var dataTable;

$(document).ready(function () {
    loadDataTable();
    
    // Search functionality
    $('#userSearch').on('keyup', function() {
        dataTable.search(this.value).draw();
    });
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: '/admin/user/getall' },
        "columns": [
            { 
                "data": "name", 
                "width": "20%",
                "render": function (data, type, row) {
                    return `<div class="d-flex align-items-center gap-2">
                        <div class="zen-order-item__avatar">${data.charAt(0).toUpperCase()}</div>
                        <span class="fw-bold">${data}</span>
                    </div>`;
                }
            },
            { "data": "email", "width": "20%" },
            { "data": "phoneNumber", "width": "15%" },
            { 
                "data": "company.name", 
                "width": "15%",
                "render": function (data) {
                    return data ? `<span class="zen-badge zen-badge--pending">${data}</span>` : '<span class="text-muted">N/A</span>';
                }
            },
            { 
                "data": "role", 
                "width": "12%",
                "render": function (data) {
                    var badgeClass = "zen-badge--pending";
                    if (data === "Admin") {
                        badgeClass = "zen-badge--cancelled";
                    } else if (data === "Employee") {
                        badgeClass = "zen-badge--approved";
                    } else if (data === "Customer") {
                        badgeClass = "zen-badge--completed";
                    }
                    return `<span class="zen-badge ${badgeClass}">${data}</span>`;
                }
            },
            {
                data: { id: "id", lockoutEnd: "lockoutEnd" },
                "render": function (data) {
                    var today = new Date().getTime();
                    var lockout = new Date(data.lockoutEnd).getTime();
                    var isLocked = lockout > today;

                    return `<div class="zen-table-actions">
                        <a onclick=LockUnlock('${data.id}') class="zen-icon-btn ${isLocked ? 'zen-icon-btn--danger' : ''}" title="${isLocked ? 'Locked' : 'Active'}">
                            <i class="bi bi-${isLocked ? 'lock-fill' : 'unlock-fill'}"></i>
                        </a>
                        <a href="/admin/user/RoleManagment?userId=${data.id}" class="zen-icon-btn" title="Manage Permissions">
                            <i class="bi bi-person-gear"></i>
                        </a>
                    </div>`;
                },
                "width": "18%"
            }
        ],
        "pageLength": 10,
        "language": {
            "search": "",
            "searchPlaceholder": "Search users...",
            "emptyTable": "No users found"
        },
        "dom": '<"row"<"col-sm-12"tr>><"row"<"col-sm-6"i><"col-sm-6"p>>'
    });
}

function LockUnlock(id) {
    $.ajax({
        type: "POST",
        url: '/Admin/User/LockUnlock',
        data: JSON.stringify(id),
        contentType: "application/json",
        success: function (data) {
            if (data.success) {
                toastr.success(data.message);
                dataTable.ajax.reload();
            }
        }
    });
}
