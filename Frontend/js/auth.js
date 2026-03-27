// ─────────────────────────────────────────────────────────────────────────────
// GreenLeaf Factory  –  Auth Module
// Manages token storage, role checks, and page guards
// ─────────────────────────────────────────────────────────────────────────────

const Auth = {
  // ── Storage keys ────────────────────────────────────────────────────────────
  KEYS: {
    token: 'gl_token',
    role:  'gl_role',
    name:  'gl_name',
    id:    'gl_id',
    email: 'gl_email',
  },

  // ── Save login response ──────────────────────────────────────────────────────
  save(loginResponse) {
    localStorage.setItem(this.KEYS.token, loginResponse.token);
    localStorage.setItem(this.KEYS.role,  loginResponse.role);
    localStorage.setItem(this.KEYS.name,  loginResponse.name);
    localStorage.setItem(this.KEYS.id,    loginResponse.id);
    localStorage.setItem(this.KEYS.email, loginResponse.email);
  },

  // ── Getters ──────────────────────────────────────────────────────────────────
  getToken() { return localStorage.getItem(this.KEYS.token); },
  getRole()  { return localStorage.getItem(this.KEYS.role);  },
  getName()  { return localStorage.getItem(this.KEYS.name);  },
  getId()    { return localStorage.getItem(this.KEYS.id);    },
  getEmail() { return localStorage.getItem(this.KEYS.email); },

  // ── Checks ───────────────────────────────────────────────────────────────────
  isLoggedIn()  { return !!this.getToken(); },
  isAdmin()     { return this.getRole() === 'Admin'; },
  isStaff()     { return this.getRole() === 'Staff'; },

  // ── Logout ───────────────────────────────────────────────────────────────────
  logout() {
    Object.values(this.KEYS).forEach(k => localStorage.removeItem(k));
    window.location.href = '/login.html';
  },

  // ── Role-based redirect after login ─────────────────────────────────────────
  redirectByRole() {
    const role = this.getRole();
    if (role === 'Admin') {
      window.location.href = '/admin/dashboard.html';
    } else if (role === 'Staff') {
      window.location.href = '/staff/dashboard.html';
    } else {
      window.location.href = '/';
    }
  },

  // ── Page guards (call at top of protected pages) ────────────────────────────
  requireLogin() {
    if (!this.isLoggedIn()) {
      window.location.href = '/login.html';
      return false;
    }
    return true;
  },

  requireAdmin() {
    if (!this.isLoggedIn()) { window.location.href = '/login.html'; return false; }
    if (!this.isAdmin())    { window.location.href = '/staff/dashboard.html'; return false; }
    return true;
  },

  requireStaff() {
    if (!this.isLoggedIn()) { window.location.href = '/login.html'; return false; }
    if (!this.isAdmin() && !this.isStaff()) {
      window.location.href = '/login.html'; return false;
    }
    return true;
  },
};

export default Auth;
