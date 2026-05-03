// ============================================================
//  EMAILJS CONFIGURATION
//  1. Go to https://www.emailjs.com and create a free account
//  2. Add an Email Service (Gmail, Outlook, etc.) → copy Service ID
//  3. Create an Email Template → copy Template ID
//  4. Go to Account → API Keys → copy your Public Key
//  5. Paste all three values below
// ============================================================

const EMAILJS_CONFIG = {
    publicKey:   "YOUR_PUBLIC_KEY",    // e.g. "user_aBcDeFgHiJkLmNoPq"
    serviceId:   "YOUR_SERVICE_ID",    // e.g. "service_abc123"
    templateId:  "YOUR_TEMPLATE_ID",   // e.g. "template_xyz789"
};

// ============================================================
//  EMAILJS TEMPLATE VARIABLES
//  When creating your template on emailjs.com, use these
//  exact variable names in your template body:
//
//  To:       {{to_email}}
//  From:     {{from_name}}
//  Subject:  {{subject}}         ← set as template subject
//  Message:  {{message}}
//  Reply-To: {{reply_to}}
// ============================================================

// Recipient email addresses — update these to real addresses
const USER_EMAILS = {
    manager:    "manager@yourcompany.com",
    accountant: "accountant@yourcompany.com",
    admin:      "admin@yourcompany.com",
};