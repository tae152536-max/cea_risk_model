/**
 * PILOT SALES — Google Forms → Customer Database
 * ================================================
 * HOW TO USE:
 *  1. Open your Google Form → click ⋮ → "Script editor"
 *  2. Paste this entire file, replacing the default code.
 *  3. Set API_URL below to your deployed CustomerAPI address.
 *  4. Save, then: Triggers → Add Trigger → onFormSubmit → On form submit
 *
 * FORM FIELDS EXPECTED (in this order):
 *   1. Dr Name
 *   2. Hospital
 *   3. Address
 *   4. Area          (dropdown matching Areas table)
 *   5. MedRep Email  (auto-filled or typed)
 *   6. Product       (Xaralto / Vissane / Arcoxia)
 *   7. Class         (A / B / C)
 */

// ── CONFIG ──────────────────────────────────────────────────────────────────
var API_URL = "http://YOUR_SERVER_IP:5000/api/customers/from-google-form";
// If running locally for testing:  http://localhost:5000/api/customers/from-google-form
// After deploying to a server:     http://192.168.1.35:5000/api/customers/from-google-form
// ────────────────────────────────────────────────────────────────────────────

/**
 * Triggered automatically when a MedRep submits the Google Form.
 */
function onFormSubmit(e) {
  var responses = e.response.getItemResponses();

  // Map answers by question title (order-independent)
  var answers = {};
  responses.forEach(function(r) {
    answers[r.getItem().getTitle().trim()] = r.getResponse();
  });

  var payload = {
    DrName:      answers["Dr Name"]       || "",
    Hospital:    answers["Hospital"]      || "",
    Address:     answers["Address"]       || "",
    Area:        answers["Area"]          || "",
    MedRepEmail: answers["MedRep Email"]  || "",
    Product:     answers["Product"]       || "",
    Class:       answers["Class"]         || "C"
  };

  if (!payload.DrName || !payload.Hospital || !payload.MedRepEmail) {
    logToSheet("ERROR", "Missing required fields", JSON.stringify(payload));
    return;
  }

  var options = {
    method: "post",
    contentType: "application/json",
    payload: JSON.stringify(payload),
    muteHttpExceptions: true
  };

  var response = UrlFetchApp.fetch(API_URL, options);
  var code     = response.getResponseCode();
  var body     = response.getContentText();

  if (code === 200) {
    logToSheet("OK", "Customer added: " + payload.DrName, body);
  } else if (code === 409) {
    // Duplicate detected
    logToSheet("DUPLICATE", "Duplicate blocked: " + payload.DrName + " @ " + payload.Hospital, body);
    sendDuplicateAlert(payload, body);
  } else {
    logToSheet("ERROR", "HTTP " + code + " for " + payload.DrName, body);
  }
}

/**
 * Write a row to a "Submission Log" sheet in the linked Spreadsheet.
 */
function logToSheet(status, message, detail) {
  var ss    = SpreadsheetApp.getActiveSpreadsheet();
  var sheet = ss.getSheetByName("Submission Log");
  if (!sheet) {
    sheet = ss.insertSheet("Submission Log");
    sheet.appendRow(["Timestamp", "Status", "Message", "Detail"]);
  }
  sheet.appendRow([new Date(), status, message, detail]);
}

/**
 * Email notification when a duplicate is blocked.
 * Change ADMIN_EMAIL to the manager's address.
 */
function sendDuplicateAlert(payload, responseBody) {
  var ADMIN_EMAIL = "manager@yourcompany.com";
  var subject     = "[DUPLICATE BLOCKED] " + payload.DrName + " @ " + payload.Hospital;
  var body = [
    "A duplicate customer submission was detected and blocked.",
    "",
    "Dr Name:      " + payload.DrName,
    "Hospital:     " + payload.Hospital,
    "Area:         " + payload.Area,
    "Submitted by: " + payload.MedRepEmail,
    "",
    "API Response: " + responseBody
  ].join("\n");

  MailApp.sendEmail(ADMIN_EMAIL, subject, body);
}
