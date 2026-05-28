# Hoang Booking – Hotel Reservation System
**Full Stack .NET Web Application | ASP.NET Core MVC | 3-Layer Architecture**

🚀 **Live Demo:** [https://hoangbooking.somee.com/](https://hoangbooking.somee.com/)

Hoang Booking is a full-featured hotel reservation web application built using **ASP.NET Core MVC**, following clean architectural principles like **3-Layer Architecture** and the **Repository Pattern**. 
The system provides room browsing, availability checking, reservation management, VNPay online payments, and a full Admin Panel for hotel staff.

---

## 🏗️ System Architecture (3-Layer Breakdown)

The application strictly follows a **3-Layer Architecture** to cleanly separate concerns, improve code maintainability, and allow for future scalability. The source code is physically divided into three independent projects:

**1. Presentation Layer (UI / Web)**
* **Role:** The user interface and the entry point of the application.
* **Components:** ASP.NET Core MVC (Controllers and Views), HTML/CSS/JS, and ViewModels.
* **Function:** It intercepts incoming HTTP requests, gathers user input, and presents data to the user (Customer or Admin). It handles UI logic, session management, and routing. This layer knows nothing about the database; it only communicates with the Business Logic Layer via Dependency Injection.

**2. Business Logic Layer (Services / BLL)**
* **Role:** The "brain" of the application where all business rules and operations reside.
* **Components:** Service classes, `GenericRepository` implementations, and third-party integration logic (like the `VNPayService`).
* **Function:** It acts as a bridge between the UI and the Data layer. It processes data received from the Presentation Layer, applies strict business rules (e.g., checking room availability before booking, calculating totals, verifying payment hashes), and calls the Data layer to persist changes.

**3. Data Access Layer (Data / DAL)**
* **Role:** Strictly responsible for database interactions and infrastructure.
* **Components:** Entity Framework Core (`AppDbContext`), Models (Entities representing database tables like Room, Reservation, Payment), and Migrations.
* **Function:** It executes CRUD (Create, Read, Update, Delete) operations against the Microsoft SQL Server database. It isolates data querying logic from the rest of the application, ensuring that if the database technology changes, the rest of the system remains unaffected.

---

## ✨ Features

### **Customer Features**
- View room types and available rooms
- Search by availability dates
- Room details page
- Add rooms to reservation cart (Session-based)
- Secure **VNPay** payment integration
- Booking confirmation & e-tickets
- Personal profile with booking history
- Contact & Messaging system
- User authentication (Register/Login)

### **Admin Panel**
- Role-based authorization (Admin / Customer)
- Manage Room Types & Rooms
- Manage Reservations & Bookings
- View and respond to Customer Contact Messages
- DataTables integration for advanced table UI
- Dashboard overview

### **Core Architectural Features**
- 3-Layer Architecture (Web, Services, Data)
- Repository Pattern for clean data access and separation of concerns
- Dependency Injection (DI)
- ASP.NET Identity for authentication and password hashing
- Entity Framework Core (Code-First Migrations)
- Automated CI/CD Pipeline

---

## 💻 Technologies & Infrastructure

### **Backend**
- ASP.NET Core MVC 8.0
- Entity Framework Core
- ASP.NET Core Identity
- VNPay Payment Gateway API

### **Frontend**
- HTML5, CSS3, JavaScript
- Bootstrap 4
- jQuery & AJAX
- DataTables
- Owl Carousel & Magnific Popup

### **DevOps & Deployment**
- **Hosting:** Somee Cloud Hosting (Windows Server IIS)
- **Database:** Microsoft SQL Server
- **CI/CD:** GitHub Actions (Automated build and FTP deployment)
- **Security:** HTTPS/SSL, Force HTTPS Redirect

### **Design Patterns**
- 3-Layer Architecture (Presentation, Business Logic, Data Access)
- Repository Pattern
- Dependency Injection
