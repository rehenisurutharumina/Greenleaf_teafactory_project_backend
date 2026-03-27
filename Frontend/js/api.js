// ─────────────────────────────────────────────────────────────────────────────
// GreenLeaf Factory  –  API Module
// Base URL: ASP.NET Core API (adjust port as needed)
// ─────────────────────────────────────────────────────────────────────────────

const API_BASE = 'http://localhost:5037/api';

// ── helpers ──────────────────────────────────────────────────────────────────

function getToken() {
  return localStorage.getItem('gl_token');
}

function authHeaders() {
  const token = getToken();
  return {
    'Content-Type': 'application/json',
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };
}

async function request(method, path, body = null) {
  const opts = {
    method,
    headers: authHeaders(),
  };
  if (body) opts.body = JSON.stringify(body);

  const res = await fetch(`${API_BASE}${path}`, opts);

  // 401 → session expired
  if (res.status === 401) {
    Auth.logout();
    return null;
  }

  if (!res.ok) {
    let err = { message: 'Request failed' };
    try { err = await res.json(); } catch (_) {}
    throw new Error(err.message || `HTTP ${res.status}`);
  }

  // 204 No Content
  if (res.status === 204) return null;

  return res.json();
}

// ── Auth ─────────────────────────────────────────────────────────────────────

const AuthAPI = {
  login: (email, password) =>
    request('POST', '/auth/login', { email, password }),

  register: (name, email, password, role = 'Staff') =>
    request('POST', '/auth/register', { name, email, password, role }),
};

// ── Products ─────────────────────────────────────────────────────────────────

const ProductsAPI = {
  getAll: () => request('GET', '/products'),
  getById: (id) => request('GET', `/products/${id}`),
  create: (data) => request('POST', '/products', data),
  update: (id, data) => request('PUT', `/products/${id}`, data),
  delete: (id) => request('DELETE', `/products/${id}`),
};

// ── Quote Requests ────────────────────────────────────────────────────────────

const QuotesAPI = {
  getAll: () => request('GET', '/quoterequests'),
  getById: (id) => request('GET', `/quoterequests/${id}`),
  create: (data) => request('POST', '/quoterequests', data),
  update: (id, data) => request('PUT', `/quoterequests/${id}`, data),
};

// ── Contact Messages ──────────────────────────────────────────────────────────

const MessagesAPI = {
  getAll: () => request('GET', '/contactmessages'),
  getById: (id) => request('GET', `/contactmessages/${id}`),
  create: (data) => request('POST', '/contactmessages', data),
  update: (id, data) => request('PUT', `/contactmessages/${id}`, data),
};

// ── Users ─────────────────────────────────────────────────────────────────────

const UsersAPI = {
  getAll: () => request('GET', '/users'),
  updateRole: (id, role) => request('PUT', `/users/${id}/role`, { role }),
  updateStatus: (id, isActive) => request('PUT', `/users/${id}/status`, { isActive }),
};

// ── Dashboard ─────────────────────────────────────────────────────────────────

const DashboardAPI = {
  getStats: () => request('GET', '/dashboard/stats'),
};

export { AuthAPI, ProductsAPI, QuotesAPI, MessagesAPI, UsersAPI, DashboardAPI };
