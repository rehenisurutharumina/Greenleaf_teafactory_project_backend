// ============================================================
// script.js — Updated Frontend with Real Backend API Calls
// ============================================================
// Replace your existing script.js with this file.
// This version connects every form and product button
// to your ASP.NET Core backend API.
// ============================================================

// ----------------------------------------------------------
// CONFIGURATION
// Change this URL if your backend runs on a different port.
// ----------------------------------------------------------
const API_BASE = "http://localhost:5001/api";

// ============================================================
// SECTION 1 — Mobile Menu Toggle
// ============================================================
const menuBtn   = document.getElementById("menuBtn");
const navLinks  = document.getElementById("navLinks");

menuBtn.addEventListener("click", () => {
  navLinks.classList.toggle("show");
});

// ============================================================
// SECTION 2 — Footer Year
// ============================================================
document.getElementById("year").textContent = new Date().getFullYear();

// ============================================================
// SECTION 3 — Load Products from Backend API
// Called automatically when the page loads.
// Replaces the static product cards with dynamic data.
// ============================================================
async function loadProducts() {
  try {
    // Call GET /api/products
    const response = await fetch(`${API_BASE}/products`);

    if (!response.ok) {
      console.error("Failed to load products:", response.status);
      return;
    }

    const products = await response.json();

    // Get the product cards container
    const container = document.querySelector("#products .cards");
    if (!container) return;

    // Clear existing static cards
    container.innerHTML = "";

    // Build a card for each product from the database
    products.forEach(product => {
      const card = document.createElement("div");
      card.className = "card product";
      card.innerHTML = `
        <div class="badge">${product.badge || ""}</div>
        <h3>${product.name}</h3>
        <p>${product.description || ""}</p>
        <p><strong>Price: $${product.pricePerKg.toFixed(2)} / kg</strong></p>
        <button class="btnSmall" data-product="${product.name}">Request Price</button>
      `;
      container.appendChild(card);
    });

    // Re-attach click handlers to the new buttons
    attachProductButtonHandlers();

  } catch (error) {
    // If backend is offline, the static cards remain (graceful fallback)
    console.warn("Could not load products from backend:", error.message);
    // Still attach handlers to static buttons
    attachProductButtonHandlers();
  }
}

// ============================================================
// SECTION 4 — Product "Request Price" Buttons
// Wires up the click handlers on all product cards.
// ============================================================
function attachProductButtonHandlers() {
  const notice = document.getElementById("notice");

  document.querySelectorAll(".btnSmall").forEach(btn => {
    btn.addEventListener("click", () => {
      const productName = btn.dataset.product;

      // Pre-fill the quote form with this product name
      const qProduct = document.getElementById("qProduct");
      if (qProduct) {
        // Try to select matching option
        for (let option of qProduct.options) {
          if (option.text.includes(productName) || productName.includes(option.text)) {
            option.selected = true;
            break;
          }
        }
      }

      // Show notice and scroll to quote form
      notice.style.display = "block";
      notice.textContent   = `Interested in ${productName}? Fill in the quote form below!`;

      // Smooth scroll to the hero section (where the quote form is)
      document.getElementById("home").scrollIntoView({ behavior: "smooth" });
    });
  });
}

// ============================================================
// SECTION 5 — Quote Request Form
// Submits to POST /api/quoterequests
// ============================================================
const quoteForm = document.getElementById("quoteForm");
const formMsg   = document.getElementById("formMsg");

quoteForm.addEventListener("submit", async (e) => {
  e.preventDefault(); // Stop the page from refreshing

  // Collect form values
  const name    = document.getElementById("qName").value.trim();
  const product = document.getElementById("qProduct").value;
  const qty     = parseInt(document.getElementById("qQty").value);

  // Simple client-side guard (server also validates)
  if (!name || !product || !qty) {
    formMsg.style.color = "red";
    formMsg.textContent = "Please fill in all fields.";
    return;
  }

  // Show loading state on the button
  const submitBtn = quoteForm.querySelector("button[type='submit']");
  submitBtn.disabled   = true;
  submitBtn.textContent = "Submitting...";
  formMsg.textContent  = "";

  try {
    // POST the form data to the backend
    const response = await fetch(`${API_BASE}/quoterequests`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json"  // Tell the API we're sending JSON
      },
      body: JSON.stringify({
        customerName: name,
        productName:  product,
        quantityKg:   qty
        // email and phone are optional — add fields in HTML if needed
      })
    });

    if (response.ok) {
      // Success!
      formMsg.style.color = "green";
      formMsg.textContent = `✅ Thanks, ${name}! We'll contact you with pricing for ${qty}kg of ${product}.`;
      quoteForm.reset();
    } else {
      // Server returned an error (e.g., 400 validation error)
      const errorData = await response.json();
      formMsg.style.color = "red";
      formMsg.textContent = "Could not submit. Please check your input.";
      console.error("Quote submit error:", errorData);
    }

  } catch (error) {
    // Network error (backend not running, etc.)
    formMsg.style.color = "orange";
    formMsg.textContent = "⚠️ Could not reach the server. Please try again later.";
    console.error("Network error:", error.message);
  } finally {
    // Always re-enable the button
    submitBtn.disabled    = false;
    submitBtn.textContent = "Submit";
  }
});

// ============================================================
// SECTION 6 — Contact Form
// Submits to POST /api/contactmessages
// ============================================================
const contactForm = document.getElementById("contactForm");
const contactMsg  = document.getElementById("contactMsg");

contactForm.addEventListener("submit", async (e) => {
  e.preventDefault();

  const email   = document.getElementById("cEmail").value.trim();
  const message = document.getElementById("cMsg").value.trim();

  if (!email || !message) {
    contactMsg.style.color = "red";
    contactMsg.textContent = "Please fill in both fields.";
    return;
  }

  const submitBtn = contactForm.querySelector("button[type='submit']");
  submitBtn.disabled    = true;
  submitBtn.textContent = "Sending...";
  contactMsg.textContent = "";

  try {
    const response = await fetch(`${API_BASE}/contactmessages`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify({
        senderEmail: email,
        message:     message
      })
    });

    if (response.ok) {
      contactMsg.style.color = "green";
      contactMsg.textContent = `✅ Message sent! We'll reply to ${email} soon.`;
      contactForm.reset();
    } else {
      const errorData = await response.json();
      contactMsg.style.color = "red";

      // EF Core validation errors come back as a nested object
      // This extracts readable error messages from the response
      if (errorData.errors) {
        const errorList = Object.values(errorData.errors).flat().join(" ");
        contactMsg.textContent = errorList;
      } else {
        contactMsg.textContent = "Could not send message. Please try again.";
      }
      console.error("Contact submit error:", errorData);
    }

  } catch (error) {
    contactMsg.style.color = "orange";
    contactMsg.textContent = "⚠️ Could not reach the server. Please try again later.";
    console.error("Network error:", error.message);
  } finally {
    submitBtn.disabled    = false;
    submitBtn.textContent = "Send";
  }
});

// ============================================================
// SECTION 7 — Initialize Page
// Load products from the API when the page first opens.
// ============================================================
loadProducts();
