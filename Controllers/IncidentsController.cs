using IncidentManagementSystem.Data;
using IncidentManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IncidentManagementSystem.Controllers;

/// <summary>
/// IncidentsController handles all CRUD operations for incidents
/// CRUD = Create, Read, Update, Delete
/// </summary>
public class IncidentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<IncidentsController> _logger;

    public IncidentsController(ApplicationDbContext context, ILogger<IncidentsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// GET: Incidents - displays list of all incidents with search and filter
    /// </summary>
    public IActionResult Index(string? searchTerm, string? statusFilter, string? priorityFilter)
    {
        try
        {
            var incidents = _context.Incidents.AsQueryable();

            // Apply search filter (by incident number or short description)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
                incidents = incidents.Where(i =>
                    i.IncidentNumber.ToLower().Contains(searchTerm) ||
                    i.ShortDescription.ToLower().Contains(searchTerm) ||
                    i.Description!.ToLower().Contains(searchTerm));
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<IncidentStatus>(statusFilter, out var status))
            {
                incidents = incidents.Where(i => i.Status == status);
            }

            // Apply priority filter
            if (!string.IsNullOrWhiteSpace(priorityFilter) && Enum.TryParse<PriorityLevel>(priorityFilter, out var priority))
            {
                incidents = incidents.Where(i => i.Priority == priority);
            }

            // Order by most recently updated first
            var sortedIncidents = incidents.OrderByDescending(i => i.UpdatedDate).ToList();

            // Store search parameters in ViewData for view
            ViewData["SearchTerm"] = searchTerm;
            ViewData["StatusFilter"] = statusFilter;
            ViewData["PriorityFilter"] = priorityFilter;

            return View(sortedIncidents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading incidents list");
            ModelState.AddModelError("", "An error occurred while loading incidents.");
            return View(new List<Incident>());
        }
    }

    /// <summary>
    /// GET: Incidents/Details/5 - displays details of a single incident
    /// </summary>
    public IActionResult Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            var incident = _context.Incidents.Find(id);
            if (incident == null)
            {
                return NotFound();
            }
            return View(incident);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error loading incident details for id {id}");
            return NotFound();
        }
    }

    /// <summary>
    /// GET: Incidents/Create - displays the create incident form
    /// </summary>
    public IActionResult Create()
    {
        return View();
    }

    /// <summary>
    /// POST: Incidents/Create - saves a new incident to the database
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create([Bind("ShortDescription,Description,Priority,AssignmentGroup")] Incident incident)
    {
        try
        {
            if (ModelState.IsValid)
            {
                // Generate incident number
                var lastIncident = _context.Incidents.OrderByDescending(i => i.Id).FirstOrDefault();
                var incidentNumber = lastIncident == null ? 1 : int.Parse(lastIncident.IncidentNumber.Split('-')[1]) + 1;
                incident.IncidentNumber = $"INC-{incidentNumber:D5}";

                // Set timestamps
                incident.CreatedDate = DateTime.UtcNow;
                incident.UpdatedDate = DateTime.UtcNow;

                _context.Add(incident);
                _context.SaveChanges();
                _logger.LogInformation($"New incident created: {incident.IncidentNumber}");

                return RedirectToAction(nameof(Index));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating incident");
            ModelState.AddModelError("", "An error occurred while creating the incident.");
        }

        return View(incident);
    }

    /// <summary>
    /// GET: Incidents/Edit/5 - displays the edit incident form
    /// </summary>
    public IActionResult Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            var incident = _context.Incidents.Find(id);
            if (incident == null)
            {
                return NotFound();
            }
            return View(incident);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error loading incident for edit: {id}");
            return NotFound();
        }
    }

    /// <summary>
    /// POST: Incidents/Edit/5 - updates an existing incident
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, [Bind("Id,IncidentNumber,ShortDescription,Description,Priority,Status,AssignmentGroup")] Incident incident)
    {
        if (id != incident.Id)
        {
            return NotFound();
        }

        try
        {
            if (ModelState.IsValid)
            {
                var existingIncident = _context.Incidents.Find(id);
                if (existingIncident == null)
                {
                    return NotFound();
                }

                // Update fields
                existingIncident.ShortDescription = incident.ShortDescription;
                existingIncident.Description = incident.Description;
                existingIncident.Priority = incident.Priority;
                existingIncident.Status = incident.Status;
                existingIncident.AssignmentGroup = incident.AssignmentGroup;
                existingIncident.UpdatedDate = DateTime.UtcNow;

                _context.Update(existingIncident);
                _context.SaveChanges();
                _logger.LogInformation($"Incident updated: {existingIncident.IncidentNumber}");

                return RedirectToAction(nameof(Details), new { id = existingIncident.Id });
            }
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, $"Concurrency error updating incident: {id}");
            ModelState.AddModelError("", "The incident was modified by another user. Please refresh and try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating incident: {id}");
            ModelState.AddModelError("", "An error occurred while updating the incident.");
        }

        return View(incident);
    }

    /// <summary>
    /// GET: Incidents/Delete/5 - displays confirmation before deleting
    /// </summary>
    public IActionResult Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            var incident = _context.Incidents.Find(id);
            if (incident == null)
            {
                return NotFound();
            }
            return View(incident);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error loading incident for delete: {id}");
            return NotFound();
        }
    }

    /// <summary>
    /// POST: Incidents/Delete/5 - deletes an incident from the database
    /// </summary>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        try
        {
            var incident = _context.Incidents.Find(id);
            if (incident != null)
            {
                var incidentNumber = incident.IncidentNumber;
                _context.Incidents.Remove(incident);
                _context.SaveChanges();
                _logger.LogInformation($"Incident deleted: {incidentNumber}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting incident: {id}");
            ModelState.AddModelError("", "An error occurred while deleting the incident.");
            return View();
        }

        return RedirectToAction(nameof(Index));
    }
}
