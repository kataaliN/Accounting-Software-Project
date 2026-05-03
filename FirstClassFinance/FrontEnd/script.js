// ===============================
// API SERVICE
// ===============================
const API_BASE_URL = "/api";

async function apiFetch(endpoint, options = {}) {
    const token = localStorage.getItem("token");
    const headers = {
        "Content-Type": "application/json",
        ...options.headers
    };
    if (token) {
        headers["Authorization"] = `Bearer ${token}`;
    }

    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
        ...options,
        headers
    });

    if (response.status === 401) {
        localStorage.removeItem("token");
        localStorage.removeItem("currentRole");
        window.location.href = "index.html";
        return;
    }

    if (!response.ok) {
        let message = "An error occurred";
        try {
            const ct = response.headers.get("content-type") || "";
            if (ct.includes("application/json")) {
                const error = await response.json();
                message = (typeof error === "string" ? error : error.message || error.title) || message;
            } else {
                message = (await response.text()).trim() || message;
            }
        } catch (_) {}
        throw new Error(message);
    }

    return response.json();
}

// ===============================
// LOGIN
// ===============================
document.getElementById("loginForm")?.addEventListener("submit", async function (e) {
    e.preventDefault();
    const username = document.getElementById("username").value.trim();
    const password = document.getElementById("password").value.trim();
    const role     = document.getElementById("role").value;

    try {
        const data = await apiFetch("/authentication/login", {
            method: "POST",
            body: JSON.stringify({ username, password, role })
        });

        localStorage.setItem("token", data.token);
        localStorage.setItem("currentRole", data.role.toLowerCase());
        localStorage.setItem("currentUsername", data.username);
        localStorage.setItem("userId", data.userId);
        // Clear any stale localStorage data so pages use the real API
        localStorage.removeItem("accounts");
        localStorage.removeItem("journals");
        localStorage.removeItem("employees");

        const roleRedirects = {
            administrator: "admin.html",
            manager: "manager.html",
            accountant: "accountant.html"
        };

        window.location.href = roleRedirects[data.role.toLowerCase()] || "index.html";
    } catch (err) {
        alert(err.message);
    }
});


// ===============================
// GLOBAL STORAGE
// ===============================
const PAGE_READONLY = document.body?.dataset.readonly === "true";

let employees      = [];
let accessRequests = [];
let accounts       = [];

function saveData() {
    localStorage.setItem("employees",      JSON.stringify(employees));
    localStorage.setItem("accessRequests", JSON.stringify(accessRequests));
    localStorage.setItem("accounts",       JSON.stringify(accounts));
}

// ===============================
// EVENT LOG HELPERS
// ===============================
function logAccountEvent(accountName, before, after, triggeredBy) {
    const eventLogs = JSON.parse(localStorage.getItem("eventLogs")) || {};
    if (!eventLogs[accountName]) eventLogs[accountName] = [];
    eventLogs[accountName].push({
        before:    before ? { ...before } : null,
        after:     { ...after },
        userId:    triggeredBy || after.userId || localStorage.getItem("currentRole") || "System",
        timestamp: new Date().toISOString()
    });
    localStorage.setItem("eventLogs", JSON.stringify(eventLogs));
}


// ===============================
// EMPLOYEE MANAGEMENT PAGE
// ===============================
async function loadEmployees() {
    try {
        employees = await apiFetch("/users");
        renderEmployees();
    } catch (err) {
        console.error("Failed to load employees:", err);
    }
}

async function renderEmployees() {
    const tbody = document.querySelector("#employeesTable tbody");
    if (!tbody) return;
    tbody.innerHTML = "";
    if (!employees.length) {
        tbody.innerHTML = `<tr><td colspan="6" style="text-align:center;color:#94a3b8;">No employees yet.</td></tr>`;
        return;
    }
    employees.forEach((emp) => {
        tbody.innerHTML += `
        <tr>
            <td>${emp.employeeUsername}</td><td>${emp.role}</td><td>${emp.emailAddress}</td>
            <td>${new Date(emp.createdAt).toLocaleDateString()}</td><td>${emp.isActive ? 'Active' : 'Inactive'}</td>
            <td><button onclick="toggleEmployee(${emp.id})"
                data-tooltip="${emp.isActive ? 'Deactivate this employee' : 'Activate this employee'}">
                ${emp.isActive ? "Deactivate" : "Activate"}
            </button></td>
        </tr>`;
    });
}

async function toggleEmployee(id) {
    try {
        await apiFetch(`/users/toggle-status/${id}`, { method: "PUT" });
        loadEmployees();
    } catch (err) {
        alert(err.message);
    }
}

document.getElementById("employeeSignupForm")?.addEventListener("submit", async function(e) {
    e.preventDefault();
    const emp = {
        firstName: document.getElementById("empFirstName").value.trim(),
        lastName:  document.getElementById("empLastName").value.trim(),
        emailAddress: document.getElementById("empEmail").value.trim(),
        dob:       document.getElementById("empDOB").value,
        address:   "N/A" // Backend requires address
    };
    try {
        await apiFetch("/users", {
            method: "POST",
            body: JSON.stringify(emp)
        });
        this.reset();
        alert(`Request submitted for ${emp.firstName} ${emp.lastName}. Status: Pending.`);
    } catch (err) {
        alert(err.message);
    }
});


// ===============================
// ACCESS REQUESTS PAGE
// ===============================
async function loadRequests() {
    try {
        accessRequests = await apiFetch("/users/pending");
        renderRequests();
    } catch (err) {
        console.error("Failed to load requests:", err);
    }
}

function renderRequests() {
    const tbody = document.querySelector("#requestsTable tbody");
    if (!tbody) return;
    tbody.innerHTML = "";
    if (!accessRequests.length) {
        tbody.innerHTML = `<tr><td colspan="6" style="text-align:center;color:#94a3b8;">No pending requests.</td></tr>`;
        return;
    }
    accessRequests.forEach((req, i) => {
        tbody.innerHTML += `
        <tr>
            <td>${req.firstName} ${req.lastName}</td><td>${req.emailAddress}</td><td>${new Date(req.dob).toLocaleDateString()}</td><td>Pending</td>
            <td><button onclick="approveRequest(${req.id})" data-tooltip="Approve — employee becomes Active">Approve</button></td>
            <td><button onclick="rejectRequest(${req.id})"  style="background:#b91c1c;" data-tooltip="Reject — removes request">Reject</button></td>
        </tr>`;
    });
}

async function approveRequest(id) {
    const role = prompt("Enter role for user (Administrator, Manager, Accountant):", "Accountant");
    const password = prompt("Enter temporary password for user:", "Password123!");
    if (!role || !password) return;

    try {
        await apiFetch(`/users/approve/${id}`, {
            method: "PUT",
            body: JSON.stringify({ role, password })
        });
        alert(`User approved.`);
        loadRequests();
    } catch (err) {
        alert(err.message);
    }
}

async function rejectRequest(id) {
    if (!confirm("Are you sure you want to REJECT and PERMANENTLY DELETE this registration request?")) return;
    try {
        await apiFetch(`/users/reject/${id}`, { method: "DELETE" });
        loadRequests();
        alert("Request rejected and deleted.");
    } catch (err) {
        alert(err.message);
    }
}


// ===============================
// ACCOUNT MANAGEMENT PAGE
// ===============================
let editingAccountId = null;
let accountFilters = { search:"", category:"", normalSide:"", status:"" };

async function loadAccounts() {
    try {
        accounts = await apiFetch("/accounts/get-all-accounts");
        renderAccounts();
    } catch (err) {
        console.error("Failed to load accounts:", err);
    }
}

// ===============================
// ACCOUNT FILTERS
// ===============================
function getFilteredAccounts() {
    const { search, category, normalSide, status } = accountFilters;
    const term = search.toLowerCase().trim();
    return accounts.filter((acc) => {
        if (category   && acc.category  !== category)   return false;
        if (normalSide && acc.normalSide !== normalSide) return false;
        if (status) {
            const isActive = status === "Active";
            if (acc.isActive !== isActive) return false;
        }
        if (term) {
            const hay = [acc.accountName, acc.accountNumber.toString(), acc.accountDescription, acc.subcategory, acc.userId.toString(), acc.comment, acc.statement].join(" ").toLowerCase();
            if (!hay.includes(term)) return false;
        }
        return true;
    });
}

function renderAccounts() {
    const tbody = document.querySelector("#accountsTable tbody");
    if (!tbody) return;
    tbody.innerHTML = "";

    if (!accounts.length) {
        tbody.innerHTML = `<tr><td colspan="16" style="text-align:center;color:#94a3b8;">No accounts yet.</td></tr>`;
        return;
    }

    const filtered = getFilteredAccounts();
    if (!filtered.length) {
        tbody.innerHTML = `<tr><td colspan="16" style="text-align:center;color:#94a3b8;">No accounts match your filters.</td></tr>`;
        return;
    }

    filtered.forEach((acc) => {
        const isEditing = acc.id === editingAccountId;
        // On readonly page, account name links to ledger.html
        const nameCell = PAGE_READONLY
            ? `<a href="ledger.html?account=${encodeURIComponent(acc.accountName)}" style="color:#60a5fa;text-decoration:none;" data-tooltip="View ledger for ${acc.accountName}">${acc.accountName}</a>`
            : `<span style="cursor:pointer;" onclick="openLedgerById(${acc.id})">${acc.accountName}</span>`;

        // Event log cell — only on readonly page
        const eventLogCell = PAGE_READONLY
            ? `<a href="event-log.html?account=${encodeURIComponent(acc.accountName)}" style="color:#a78bfa;text-decoration:none;font-size:0.8rem;" data-tooltip="View event log for ${acc.accountName}">📋 Log</a>`
            : "";

        const rowClick = PAGE_READONLY ? "" : `onclick="openLedgerById(${acc.id})" style="cursor:pointer;" title="Click to view ledger"`;

        tbody.innerHTML += `
        <tr ${rowClick} data-id="${acc.id}" class="${isEditing ? 'editing-row' : ''}">
            <td>${nameCell}</td>
            <td>${acc.accountNumber}</td>
            <td>${acc.accountDescription}</td>
            <td>${acc.normalSide}</td>
            <td>${acc.category}</td>
            <td>${acc.subcategory}</td>
            <td>${parseFloat(acc.debit  ||0).toFixed(2)}</td>
            <td>${parseFloat(acc.credit ||0).toFixed(2)}</td>
            <td>${parseFloat(acc.balance||0).toFixed(2)}</td>
            <td>${new Date(acc.dateAdded).toLocaleDateString()}</td>
            <td>${acc.userId}</td>
            <td>${acc.order}</td>
            <td>${acc.statement}</td>
            <td>${acc.comment}</td>
            <td>${acc.isActive ? 'Active' : 'Inactive'}</td>
            ${PAGE_READONLY ? `<td>${eventLogCell}</td>` : ""}
        </tr>`;
    });
}

function loadAccountIntoForm(id) {
    const acc = accounts.find(a => a.id === id);
    if (!acc) return;
    editingAccountId = id;
    document.getElementById("accountName").value        = acc.accountName   || "";
    document.getElementById("accountNumber").value      = acc.accountNumber || "";
    document.getElementById("accountDescription").value = acc.accountDescription || "";
    document.getElementById("normalSide").value         = acc.normalSide    || "Debit";
    document.getElementById("accountCategory").value    = acc.category      || "Asset";
    document.getElementById("accountSubcategory").value = acc.subcategory   || "";
    document.getElementById("initialBalance").value     = acc.initialBalance || 0;
    document.getElementById("debit").value              = acc.debit         || 0;
    document.getElementById("credit").value             = acc.credit        || 0;
    document.getElementById("balance").value            = acc.balance       || 0;
    document.getElementById("userId").value             = acc.userId        || "";
    document.getElementById("accountOrder").value       = acc.order         || "";
    document.getElementById("statementType").value      = acc.statement     || "BS";
    document.getElementById("comment").value            = acc.comment       || "";

    const title  = document.querySelector(".right-panel h2");
    const submit = document.querySelector("#createAccountForm button[type='submit']");
    const cancel = document.getElementById("cancelEditBtn");
    if (title)  title.textContent  = `Editing: ${acc.accountName}`;
    if (submit) submit.textContent = "Save Changes";
    if (cancel) cancel.style.display = "block";

    renderAccounts();
    document.querySelector(".right-panel")?.scrollIntoView({ behavior:"smooth", block:"start" });
}

function resetAccountForm() {
    editingAccountId = null;
    const form   = document.getElementById("createAccountForm");
    const title  = document.querySelector(".right-panel h2");
    const submit = document.querySelector("#createAccountForm button[type='submit']");
    const cancel = document.getElementById("cancelEditBtn");
    if (form)   form.reset();
    if (title)  title.textContent  = "Create Account";
    if (submit) submit.textContent = "Create Account";
    if (cancel) cancel.style.display = "none";
    renderAccounts();
}

document.getElementById("createAccountForm")?.addEventListener("submit", async function(e) {
    e.preventDefault();
    const data = {
        accountName:   document.getElementById("accountName").value.trim(),
        accountNumber: parseInt(document.getElementById("accountNumber").value),
        accountDescription: document.getElementById("accountDescription").value.trim(),
        normalSide:    document.getElementById("normalSide").value,
        category:      document.getElementById("accountCategory").value,
        subcategory:   document.getElementById("accountSubcategory").value.trim(),
        initialBalance: parseFloat(document.getElementById("initialBalance").value || 0),
        debit:         parseFloat(document.getElementById("debit").value   || 0),
        credit:        parseFloat(document.getElementById("credit").value  || 0),
        balance:       parseFloat(document.getElementById("balance").value || 0),
        userId:        parseInt(document.getElementById("userId").value || 0),
        order:         parseInt(document.getElementById("accountOrder").value || 0),
        statement:     document.getElementById("statementType").value,
        comment:       document.getElementById("comment").value.trim(),
        isActive:      true
    };

    try {
        if (editingAccountId !== null) {
            await apiFetch(`/accounts/${editingAccountId}`, {
                method: "PUT",
                body: JSON.stringify(data)
            });
            alert(`Account "${data.accountName}" updated.`);
        } else {
            await apiFetch("/accounts/add-account", {
                method: "POST",
                body: JSON.stringify(data)
            });
            alert(`Account "${data.accountName}" created.`);
        }
        resetAccountForm();
        loadAccounts();
    } catch (err) {
        alert(err.message);
    }
});


// ===============================
// LEDGER POPUP (admin account-management page only)
// ===============================
function openLedgerById(id) {
    const modal   = document.getElementById("ledgerModal");
    const content = document.getElementById("ledgerContent");
    if (!modal || !content) return;
    const acc = accounts.find(a => a.id === id);
    if (!acc) return;
    content.innerHTML = `
        <table style="width:100%;border-collapse:collapse;">
            <tr><td style="padding:8px;font-weight:bold;color:#94a3b8;">Account Name</td>   <td style="padding:8px;color:#e2e8f0;">${acc.accountName}</td></tr>
            <tr><td style="padding:8px;font-weight:bold;color:#94a3b8;">Account Number</td> <td style="padding:8px;color:#e2e8f0;">${acc.accountNumber}</td></tr>
            <tr><td style="padding:8px;font-weight:bold;color:#94a3b8;">Description</td>    <td style="padding:8px;color:#e2e8f0;">${acc.accountDescription}</td></tr>
            <tr><td style="padding:8px;font-weight:bold;color:#94a3b8;">Category</td>       <td style="padding:8px;color:#e2e8f0;">${acc.category}</td></tr>
            <tr><td style="padding:8px;font-weight:bold;color:#94a3b8;">Subcategory</td>    <td style="padding:8px;color:#e2e8f0;">${acc.subcategory}</td></tr>
            <tr><td style="padding:8px;font-weight:bold;color:#94a3b8;">Normal Side</td>    <td style="padding:8px;color:#e2e8f0;">${acc.normalSide}</td></tr>
            <tr><td style="padding:8px;font-weight:bold;color:#94a3b8;">Debit</td>          <td style="padding:8px;color:#e2e8f0;">$${parseFloat(acc.debit ||0).toFixed(2)}</td></tr>
            <tr><td style="padding:8px;font-weight:bold;color:#94a3b8;">Credit</td>         <td style="padding:8px;color:#e2e8f0;">$${parseFloat(acc.credit||0).toFixed(2)}</td></tr>
            <tr><td style="padding:8px;font-weight:bold;color:#94a3b8;">Balance</td>        <td style="padding:8px;font-weight:bold;color:#34d399;">$${parseFloat(acc.balance||0).toFixed(2)}</td></tr>
            <tr><td style="padding:8px;font-weight:bold;color:#94a3b8;">Statement</td>      <td style="padding:8px;color:#e2e8f0;">${acc.statement}</td></tr>
            <tr><td style="padding:8px;font-weight:bold;color:#94a3b8;">Status</td>         <td style="padding:8px;color:#e2e8f0;">${acc.isActive ? 'Active' : 'Inactive'}</td></tr>
            <tr><td style="padding:8px;font-weight:bold;color:#94a3b8;">Added By</td>       <td style="padding:8px;color:#e2e8f0;">${acc.userId}</td></tr>
            <tr><td style="padding:8px;font-weight:bold;color:#94a3b8;">Date Added</td>     <td style="padding:8px;color:#e2e8f0;">${new Date(acc.dateAdded).toLocaleDateString()}</td></tr>
            <tr><td style="padding:8px;font-weight:bold;color:#94a3b8;">Comment</td>        <td style="padding:8px;color:#e2e8f0;">${acc.comment}</td></tr>
        </table>
        <div style="margin-top:16px;display:flex;gap:10px;flex-wrap:wrap;">
            ${PAGE_READONLY ? '' : `<button onclick="closeLedgerAndEdit(${acc.id})" style="background:#047857;margin-top:0;" data-tooltip="Edit this account">&#9998; Edit Account</button>`}
            <a href="ledger.html?account=${encodeURIComponent(acc.accountName)}" style="display:inline-block;padding:10px 16px;background:#1d4ed8;color:#fff;text-decoration:none;border-radius:6px;font-size:0.875rem;font-weight:600;" data-tooltip="View full ledger for this account">📒 View Ledger</a>
            <button onclick="document.getElementById('ledgerModal').style.display='none'" style="background:#374151;margin-top:0;" data-tooltip="Close">Close</button>
        </div>`;
    modal.style.display = "block";
}

function closeLedgerAndEdit(id) {
    document.getElementById("ledgerModal").style.display = "none";
    loadAccountIntoForm(id);
}

document.querySelector(".close")?.addEventListener("click", () => { document.getElementById("ledgerModal").style.display = "none"; });
window.addEventListener("click", e => { const m = document.getElementById("ledgerModal"); if (m && e.target === m) m.style.display = "none"; });


// ===============================
// PAGE LOAD
// ===============================
document.addEventListener("DOMContentLoaded", function () {
    const isLoginPage = !!document.getElementById("loginForm");

    if (!isLoginPage) {
        loadEmployees();
        loadRequests();
        loadAccounts();
    }

    document.getElementById("cancelEditBtn")?.addEventListener("click", resetAccountForm);

    // ─── Read-only page role setup ──────────────────────────────
    if (PAGE_READONLY) {
        const role = localStorage.getItem("currentRole") || "manager";
        const cfg = {
            manager: {
                name:"Manager", label:"Manager", avatar:"M",
                img:"Assets/managerProfileImage.png",
                badgeStyle:"color:#60a5fa;background:rgba(96,165,250,0.12);border-color:rgba(96,165,250,0.3);",
                avatarStyle:"background:linear-gradient(135deg,#1d4ed8,#1e40af);color:#bfdbfe;border-color:rgba(96,165,250,0.3);",
                nav:[
                    { href:"manager.html",                     icon:"⊞",  label:"Dashboard" },
                    { href:"account-management-readonly.html", icon:"📒", label:"Chart of Accounts", active:true },
                    { href:"journal.html",                     icon:"📝", label:"Journal Transactions" },
                    { href:"reports.html",                                icon:"📊", label:"Financial Reports" },
                    { href:"inbox.html",                                  icon:"📬", label:"Inbox" },
                ]
            },
            accountant: {
                name:"Accountant", label:"Accountant", avatar:"A",
                img:"Assets/accountantProfileImage.png",
                badgeStyle:"color:#a78bfa;background:rgba(167,139,250,0.12);border-color:rgba(167,139,250,0.3);",
                avatarStyle:"background:linear-gradient(135deg,#6d28d9,#5b21b6);color:#ddd6fe;border-color:rgba(167,139,250,0.3);",
                nav:[
                    { href:"accountant.html",                  icon:"⊞",  label:"Dashboard" },
                    { href:"account-management-readonly.html", icon:"📒", label:"Chart of Accounts", active:true },
                    { href:"journal-accountant.html",          icon:"✏️", label:"Journal Entries" },
                    { href:"inbox.html",                                  icon:"📬", label:"Inbox" },
                ]
            }
        };
        const c = cfg[role] || cfg.manager;
        const set = (id, val) => { const el = document.getElementById(id); if (el) el.textContent = val; };
        const setStyle = (id, s) => { const el = document.getElementById(id); if (el) el.style.cssText += s; };
        set("profileAvatar", c.avatar);       setStyle("profileAvatar", c.avatarStyle);
        set("profileName", c.name);
        set("profileRoleLabel", c.label);
        set("profileTriggerName", c.name);
        const img = document.getElementById("profileImg"); if (img) img.src = c.img;
        const badge = document.getElementById("roleBadge"); if (badge) { badge.textContent = c.label; badge.style.cssText = c.badgeStyle; }
        const nav = document.getElementById("roledNav");
        if (nav) nav.innerHTML = c.nav.map(n => `<a href="${n.href}" class="waffle-nav-item${n.active?' active':''}"><span class="waffle-nav-icon">${n.icon}</span><span>${n.label}</span></a>`).join("");
    }

    // ─── Help Modal ──────────────────────────────────────────────
    const helpBtn=document.getElementById("helpBtn"), helpModal=document.getElementById("helpModal"), helpOverlay=document.getElementById("helpOverlay"), helpClose=document.getElementById("helpModalClose");
    const openHelp=()=>{ helpModal?.classList.add("open"); helpOverlay?.classList.add("active"); };
    const closeHelp=()=>{ helpModal?.classList.remove("open"); helpOverlay?.classList.remove("active"); };
    helpBtn?.addEventListener("click", openHelp);
    helpClose?.addEventListener("click", closeHelp);
    helpOverlay?.addEventListener("click", closeHelp);
    document.querySelectorAll(".help-topic-toggle").forEach(btn => {
        btn.addEventListener("click", function() {
            const body = this.nextElementSibling, isOpen = body.classList.contains("open");
            document.querySelectorAll(".help-topic-body").forEach(b => b.classList.remove("open"));
            document.querySelectorAll(".help-topic-toggle").forEach(b => b.classList.remove("active"));
            if (!isOpen) { body.classList.add("open"); this.classList.add("active"); }
        });
    });

    // ─── Custom Tooltips ─────────────────────────────────────────
    const tip = document.createElement("div"); tip.className = "custom-tooltip"; document.body.appendChild(tip);
    let tipTimeout;
    document.addEventListener("mouseover", e => {
        const t = e.target.closest("[data-tooltip]"); if (!t) return;
        clearTimeout(tipTimeout);
        tipTimeout = setTimeout(() => { tip.textContent = t.getAttribute("data-tooltip"); tip.classList.add("visible"); posTip(e); }, 320);
    });
    document.addEventListener("mousemove", e => { if (tip.classList.contains("visible")) posTip(e); });
    document.addEventListener("mouseout",  e => { const t = e.target.closest("[data-tooltip]"); if (!t) return; clearTimeout(tipTimeout); tip.classList.remove("visible"); });
    function posTip(e) {
        const p=12; let x=e.clientX+p, y=e.clientY+p;
        if (x+tip.offsetWidth  > window.innerWidth  - 8) x = e.clientX - tip.offsetWidth  - p;
        if (y+tip.offsetHeight > window.innerHeight - 8) y = e.clientY - tip.offsetHeight - p;
        tip.style.left = x+"px"; tip.style.top = y+"px";
    }

    // ─── Account Filters ─────────────────────────────────────────
    document.getElementById("filterSearch")?.addEventListener("input",    function() { accountFilters.search     = this.value;  renderAccounts(); });
    document.getElementById("filterCategory")?.addEventListener("change", function() { accountFilters.category   = this.value;  renderAccounts(); });
    document.getElementById("filterNormalSide")?.addEventListener("change",function(){ accountFilters.normalSide = this.value;  renderAccounts(); });
    document.getElementById("filterStatus")?.addEventListener("change",   function() { accountFilters.status     = this.value;  renderAccounts(); });
    document.getElementById("filterClearBtn")?.addEventListener("click",  function() {
        accountFilters = { search:"", category:"", normalSide:"", status:"" };
        ["filterSearch","filterCategory","filterNormalSide","filterStatus"].forEach(id => { const el = document.getElementById(id); if (el) el.value = ""; });
        renderAccounts();
    });

    // ─── Waffle Sidebar ───────────────────────────────────────────
    const wBtn=document.getElementById("waffleBtn"), wSide=document.getElementById("waffleSidebar"), wOver=document.getElementById("sidebarOverlay"), wClose=document.getElementById("waffleSidebarClose");
    const openSide=()=>{ wSide?.classList.add("open"); wOver?.classList.add("active"); };
    const closeSide=()=>{ wSide?.classList.remove("open"); wOver?.classList.remove("active"); };
    wBtn?.addEventListener("click", openSide);
    wClose?.addEventListener("click", closeSide);
    wOver?.addEventListener("click", closeSide);

    // ─── Profile Dropdown ─────────────────────────────────────────
    const logoutBtn = document.querySelector(".profile-dropdown-item");
    logoutBtn?.addEventListener("click", function(e) {
        localStorage.removeItem("token");
        localStorage.removeItem("currentRole");
        // No need to preventDefault if it's already an <a> to index.html, 
        // but we want to ensure cleanup.
    });

    const pTrig=document.getElementById("profileTrigger"), pDrop=document.getElementById("profileDropdown");
    pTrig?.addEventListener("click", e => { e.stopPropagation(); const o = pDrop?.classList.toggle("open"); pTrig.classList.toggle("open", o); });
    document.addEventListener("click", e => { if (!pDrop?.contains(e.target) && !pTrig?.contains(e.target)) { pDrop?.classList.remove("open"); pTrig?.classList.remove("open"); } });

    // ─── Login Page Modals ─────────────────────────────────────────
    const forgotModal = document.getElementById("forgotPasswordModal");
    const requestModal = document.getElementById("requestAccessModal");

    document.getElementById("forgotPasswordLink")?.addEventListener("click", (e) => {
        e.preventDefault();
        forgotModal.style.display = "block";
    });

    document.getElementById("requestAccessLink")?.addEventListener("click", (e) => {
        e.preventDefault();
        requestModal.style.display = "block";
    });

    document.getElementById("closeForgotPassword")?.addEventListener("click", () => {
        forgotModal.style.display = "none";
        document.getElementById("forgotPasswordForm").style.display = "block";
        document.getElementById("resetPasswordForm").style.display = "none";
    });

    document.getElementById("closeRequestAccess")?.addEventListener("click", () => {
        requestModal.style.display = "none";
    });

    window.addEventListener("click", (e) => {
        if (e.target === forgotModal) forgotModal.style.display = "none";
        if (e.target === requestModal) requestModal.style.display = "none";
    });

    // ─── Forgot Password Logic ────────────────────────────────────
    document.getElementById("forgotPasswordForm")?.addEventListener("submit", async function(e) {
        e.preventDefault();
        const username = document.getElementById("forgotUsername").value.trim();
        const emailAddress = document.getElementById("forgotEmail").value.trim();

        try {
            const data = await apiFetch("/authentication/forgot-password", {
                method: "POST",
                body: JSON.stringify({ username, emailAddress })
            });
            document.getElementById("securityQuestionDisplay").textContent = `Security Question: ${data.securityQuestion || "Not set. Contact admin."}`;
            this.style.display = "none";
            document.getElementById("resetPasswordForm").style.display = "block";
        } catch (err) {
            alert(err.message);
        }
    });

    document.getElementById("resetPasswordForm")?.addEventListener("submit", async function(e) {
        e.preventDefault();
        const username = document.getElementById("forgotUsername").value.trim();
        const emailAddress = document.getElementById("forgotEmail").value.trim();
        const securityAnswer = document.getElementById("securityAnswer").value.trim();
        const newPassword = document.getElementById("newPassword").value.trim();

        try {
            await apiFetch("/authentication/reset-password", {
                method: "POST",
                body: JSON.stringify({ username, emailAddress, securityAnswer, newPassword })
            });
            alert("Password reset successfully! You can now log in.");
            forgotModal.style.display = "none";
            this.reset();
            document.getElementById("forgotPasswordForm").reset();
            document.getElementById("forgotPasswordForm").style.display = "block";
            this.style.display = "none";
        } catch (err) {
            alert(err.message);
        }
    });

    // ─── Public Request Access Logic ──────────────────────────────
    document.getElementById("publicSignupForm")?.addEventListener("submit", async function(e) {
        e.preventDefault();
        const emp = {
            firstName: document.getElementById("signupFirstName").value.trim(),
            lastName:  document.getElementById("signupLastName").value.trim(),
            emailAddress: document.getElementById("signupEmail").value.trim(),
            dob:       document.getElementById("signupDOB").value,
            address:   "N/A"
        };
        try {
            await apiFetch("/users", {
                method: "POST",
                body: JSON.stringify(emp)
            });
            this.reset();
            requestModal.style.display = "none";
            alert(`Request submitted for ${emp.firstName} ${emp.lastName}. An email notification has been sent to the administrator.`);
        } catch (err) {
            alert(err.message);
        }
    });
});