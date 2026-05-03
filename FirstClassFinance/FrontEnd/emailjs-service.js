// ============================================================
//  SHARED EMAIL SERVICE
//  Wraps EmailJS so every page calls one function.
//  Requires emailjs-config.js to be loaded first.
// ============================================================

// Load EmailJS SDK from CDN and initialize
(function initEmailJS() {
    if (window.emailjs) {
        emailjs.init(EMAILJS_CONFIG.publicKey);
        return;
    }
    const script   = document.createElement("script");
    script.src     = "https://cdn.jsdelivr.net/npm/@emailjs/browser@3/dist/email.min.js";
    script.async   = true;
    script.onload  = () => emailjs.init(EMAILJS_CONFIG.publicKey);
    script.onerror = () => console.error("EmailJS SDK failed to load.");
    document.head.appendChild(script);
})();

/**
 * Send an email via EmailJS.
 * @param {string} toEmail   - recipient email address
 * @param {string} toName    - recipient display name
 * @param {string} subject   - email subject line
 * @param {string} message   - email body (plain text)
 * @param {string} fromName  - sender display name (default: "First Class Finance")
 * @returns {Promise}
 */
async function sendEmail(toEmail, toName, subject, message, fromName = "First Class Finance — Admin") {
    if (!window.emailjs) throw new Error("EmailJS not loaded yet.");

    const params = {
        to_email:  toEmail,
        to_name:   toName,
        from_name: fromName,
        subject:   subject,
        message:   message,
        reply_to:  USER_EMAILS.admin,
    };

    return emailjs.send(EMAILJS_CONFIG.serviceId, EMAILJS_CONFIG.templateId, params);
}

/**
 * Send a journal submission notification to the manager.
 * Called automatically when accountant submits a journal entry.
 * @param {object} entry - the journal entry object
 */
async function notifyManagerOfJournalSubmission(entry) {
    const drLines = entry.debitLines  || [{ account: entry.debitAccount,  amount: entry.debit  }];
    const crLines = entry.creditLines || [{ account: entry.creditAccount, amount: entry.credit }];
    const drTotal = drLines.reduce((s, l) => s + parseFloat(l.amount), 0);

    const subject = `[First Class Finance] New Journal Entry ${entry.pr} Awaiting Approval`;
    const message =
        `A new journal entry has been submitted and requires your approval.

PR Number:    ${entry.pr}
Date:         ${entry.date}
Description:  ${entry.description || "—"}
Created By:   ${entry.createdBy}
Total Amount: $${drTotal.toFixed(2)}

Debit Lines:
${drLines.map(l => `  Dr: ${l.account}  $${parseFloat(l.amount).toFixed(2)}`).join("\n")}

Credit Lines:
${crLines.map(l => `  Cr: ${l.account}  $${parseFloat(l.amount).toFixed(2)}`).join("\n")}

Please log in to First Class Finance to review and approve or reject this entry.`;

    return sendEmail(USER_EMAILS.manager, "Manager", subject, message, `${entry.createdBy} via First Class Finance`);
}

/**
 * Get unread notification count for the current role.
 */
function getUnreadNotificationCount() {
    const notifs = JSON.parse(localStorage.getItem("managerNotifications")) || [];
    return notifs.filter(n => !n.read).length;
}

/**
 * Mark all manager notifications as read.
 */
function markNotificationsRead() {
    const notifs = JSON.parse(localStorage.getItem("managerNotifications")) || [];
    notifs.forEach(n => n.read = true);
    localStorage.setItem("managerNotifications", JSON.stringify(notifs));
}