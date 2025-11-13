using LoginandRegisterMVC.Data;
using LoginandRegisterMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LoginandRegisterMVC.Controllers;

/// <summary>
/// Controller for managing user roles
/// </summary>
[Authorize(Roles = "Admin")]
public class RolesController(
    UserContext context,
    ILogger<RolesController> logger) : Controller
{
    private readonly UserContext _context = context;
    private readonly ILogger<RolesController> _logger = logger;

    /// <summary>
    /// GET: /Roles/Index - Display all roles
    /// </summary>
    public async Task<IActionResult> Index()
    {
        try
        {
            _logger.LogInformation("Loading roles list");
            var roles = await _context.Roles.OrderBy(r => r.RoleName).ToListAsync();
            return View(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading roles list");
            TempData["Error"] = "An error occurred while loading roles.";
            return RedirectToAction("Index", "Users");
        }
    }

    /// <summary>
    /// GET: /Roles/Create - Display create role form
    /// </summary>
    public IActionResult Create()
    {
        return View();
    }

    /// <summary>
    /// POST: /Roles/Create - Create a new custom role
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Role role)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(role);
            }

            // Check if role already exists
            if (await _context.Roles.AnyAsync(r => r.RoleName == role.RoleName))
            {
                ModelState.AddModelError("RoleName", "A role with this name already exists.");
                return View(role);
            }

            role.IsSystemRole = false;
            role.CreatedAt = DateTime.UtcNow;
            role.UpdatedAt = DateTime.UtcNow;

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new role: {RoleName}", role.RoleName);
            TempData["Success"] = $"Role '{role.RoleName}' created successfully!";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role");
            TempData["Error"] = "An error occurred while creating the role.";
            return View(role);
        }
    }

    /// <summary>
    /// GET: /Roles/Edit/roleName - Display edit role form
    /// </summary>
    public async Task<IActionResult> Edit(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["Error"] = "Role name is required.";
                return RedirectToAction("Index");
            }

            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                TempData["Error"] = "Role not found.";
                return RedirectToAction("Index");
            }

            if (role.IsSystemRole)
            {
                TempData["Error"] = "System roles cannot be edited.";
                return RedirectToAction("Index");
            }

            return View(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading role for editing: {RoleName}", id);
            TempData["Error"] = "An error occurred while loading the role.";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// POST: /Roles/Edit - Update a custom role
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Role role)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(role);
            }

            var existingRole = await _context.Roles.FindAsync(role.RoleName);
            if (existingRole == null)
            {
                TempData["Error"] = "Role not found.";
                return RedirectToAction("Index");
            }

            if (existingRole.IsSystemRole)
            {
                TempData["Error"] = "System roles cannot be modified.";
                return RedirectToAction("Index");
            }

            existingRole.Description = role.Description;
            existingRole.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated role: {RoleName}", role.RoleName);
            TempData["Success"] = $"Role '{role.RoleName}' updated successfully!";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role: {RoleName}", role.RoleName);
            TempData["Error"] = "An error occurred while updating the role.";
            return View(role);
        }
    }

    /// <summary>
    /// POST: /Roles/Delete - Delete a custom role
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string roleName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                TempData["Error"] = "Role name is required.";
                return RedirectToAction("Index");
            }

            var role = await _context.Roles.FindAsync(roleName);
            if (role == null)
            {
                TempData["Error"] = "Role not found.";
                return RedirectToAction("Index");
            }

            if (role.IsSystemRole)
            {
                TempData["Error"] = "System roles cannot be deleted.";
                return RedirectToAction("Index");
            }

            // Check if any users have this role
            var usersWithRole = await _context.Users.CountAsync(u => u.Role == roleName);
            if (usersWithRole > 0)
            {
                TempData["Error"] = $"Cannot delete role '{roleName}' because {usersWithRole} user(s) are assigned to it.";
                return RedirectToAction("Index");
            }

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted role: {RoleName}", roleName);
            TempData["Success"] = $"Role '{roleName}' deleted successfully!";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role: {RoleName}", roleName);
            TempData["Error"] = "An error occurred while deleting the role.";
            return RedirectToAction("Index");
        }
    }
}
