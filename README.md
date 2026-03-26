# GreenLeaf Tea Factory Management System

GreenLeaf Tea Factory Management System is a full-stack web application designed to manage tea products, customer orders, and factory operations in a modern and organized way.

This project is being rebuilt from scratch to provide a cleaner architecture, better maintainability, and a smoother user experience.

---

## Project Overview

The system supports three main user roles:

- **Admin** – manages users, products, orders, and overall system operations
- **Factory Staff** – handles order processing, production-related updates, and operational tasks
- **Customer** – browses tea products, places orders, and tracks their requests

The goal of this project is to digitalize tea factory operations while also providing an online platform for customers to interact with the business.

---

## Main Features

### Customer Features
- Register and log in
- Browse tea products
- View product details
- Add products to cart
- Place orders
- Track order status
- Submit contact or inquiry messages

### Factory Staff Features
- Secure login
- View assigned orders
- Update order progress
- Manage operational workflows
- View customer order details

### Admin Features
- Manage users
- Manage tea products
- Manage categories
- View and manage orders
- Monitor staff activities
- Access dashboard and reports

---

## Tech Stack

### Frontend
- HTML
- CSS
- JavaScript

### Backend
- ASP.NET Core Web API
- Entity Framework Core

### Database
- MySQL

### Tools & Packages
- Pomelo EntityFrameworkCore MySQL Provider
- Swagger / OpenAPI
- CORS Configuration
- REST API Architecture

---

## Project Structure

```bash
GreenLeaf-Tea-Factory/
│
├── frontend/                  # Static frontend (HTML, CSS, JavaScript)
│   ├── index.html
│   ├── pages/
│   ├── css/
│   ├── js/
│   └── assets/
│
├── backend/                   # ASP.NET Core Web API
│   ├── Controllers/
│   ├── Models/
│   ├── DTOs/
│   ├── Data/
│   ├── Services/
│   ├── Repositories/
│   ├── Properties/
│   ├── appsettings.json
│   └── Program.cs
│
└── README.md
