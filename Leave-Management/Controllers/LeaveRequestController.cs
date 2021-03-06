﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Leave_Management.Contracts;
using Leave_Management.Data;
using Leave_Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Leave_Management.Controllers
{
    [Authorize]
    public class LeaveRequestController : Controller
    {
        private readonly ILeaveTypeRepository _leaveTypeRepo;
        private readonly ILeaveAllocationRepository _leaveAllocationRepo;
        private readonly ILeaveRequestRepository _leaveRequestRepo;
        private readonly IMapper _mapper;
        private readonly UserManager<Employee> _userManager;

        public LeaveRequestController(
            ILeaveTypeRepository leaveTypeRepo,
            ILeaveAllocationRepository leaveAllocationRepo,
            ILeaveRequestRepository leaveRequestRepo,
            IMapper mapper,
            UserManager<Employee> userManager)
        {
            _leaveTypeRepo = leaveTypeRepo;
            _leaveAllocationRepo = leaveAllocationRepo;
            _leaveRequestRepo = leaveRequestRepo;
            _mapper = mapper;
            _userManager = userManager;
        }

        [Authorize(Roles = "Administrator")]
        // GET: LeaveRequest
        public async Task<ActionResult> Index()
        {
            var leaveRequests = await _leaveRequestRepo.FindAll();
            var leaveRequestsModel = _mapper.Map<List<LeaveRequestVM>>(leaveRequests);

            var model = new AdminLeaveRequestViewVM
            {
                TotalRequests = leaveRequests.Count,
                ApprovedRequests = leaveRequests.Count(q => q.Approved == true),
                PendingRequests = leaveRequests.Count(q => q.Approved == null),
                RejectedRequests = leaveRequests.Count(q => q.Approved == false),
                LeaveRequests =leaveRequestsModel
            };

            return View(model);
        }

        // GET: LeaveRequest/Details/5
        public ActionResult Details(int id)
        {
            var leaveRequest = _leaveRequestRepo.FindById(id);
            var model = _mapper.Map<LeaveRequestVM>(leaveRequest);

            return View(model);
        }

        public async Task<ActionResult> ApproveRequest(int id)
        {
            try
            {
                var approvingUser = _userManager.GetUserAsync(User).Result;
                var leaveRequest = await _leaveRequestRepo.FindById(id);

                var employeeId = leaveRequest.RequestingEmployee.Id;
                var leaveTypeId = leaveRequest.LeaveTypeId;
                var allocation = await _leaveAllocationRepo
                    .GetLeaveAllocationsByEmployeeAndType(employeeId, leaveTypeId);

                var startDate = Convert.ToDateTime(leaveRequest.StartDate);
                var endDate = Convert.ToDateTime(leaveRequest.EndDate);
                int daysRequested = (int)(endDate - startDate).TotalDays;
                allocation.NumberOfDays -= daysRequested;

                leaveRequest.ApprovedById = approvingUser.Id;
                leaveRequest.Approved = true;
                leaveRequest.DateActioned = DateTime.Now;

                await _leaveRequestRepo.Update(leaveRequest);
                await _leaveAllocationRepo.Update(allocation);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index));
            }
        }
        
        public async Task<ActionResult> RejectRequest(int id)
        {
            try
            {
                var approvingUser = _userManager.GetUserAsync(User).Result;
                var leaveRequest = await _leaveRequestRepo.FindById(id);

                leaveRequest.ApprovedById = approvingUser.Id;
                leaveRequest.Approved = false;
                leaveRequest.DateActioned = DateTime.Now;

                await _leaveRequestRepo.Update(leaveRequest);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: LeaveRequest/Create
        public async Task<ActionResult> Create()
        {
            var leaveTypes = await _leaveTypeRepo.FindAll();
            var leaveTypeItems = leaveTypes.Select(q => new SelectListItem
            {
                Text = q.Name,
                Value = q.Id.ToString()
            });

            var model = new CreateLeaveRequestVM
            {
                LeaveTypes = leaveTypeItems
            };

            return View(model);
        }

        // POST: LeaveRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateLeaveRequestVM model)
        { 
            try
            {
                var leaveTypes = await _leaveTypeRepo.FindAll();
                var leaveTypeItems = leaveTypes.Select(q => new SelectListItem
                {
                    Text = q.Name,
                    Value = q.Id.ToString()
                });
                model.LeaveTypes = leaveTypeItems;

                if (!ModelState.IsValid)
                    return View(model);

                var startDate = Convert.ToDateTime(model.StartDate);
                var endDate = Convert.ToDateTime(model.EndDate);

                if (DateTime.Compare(startDate, endDate) > 1)
                {
                    ModelState.AddModelError("", "Start Date cannot be further in the future than End Date.");
                    return View(model);
                }

                var employee = _userManager.GetUserAsync(User).Result;
                var allocation = await _leaveAllocationRepo
                    .GetLeaveAllocationsByEmployeeAndType(employee.Id, model.LeaveTypeId);

                int daysRequested = (int)(endDate - startDate).TotalDays;
                if(daysRequested > allocation.NumberOfDays)
                {
                    ModelState.AddModelError("", "You do not have sufficient days for this request.");
                    return View(model);
                }

                var leaveRequestModel = new LeaveRequestVM
                {
                    RequestingEmployeeId = employee.Id,
                    StartDate = startDate,
                    EndDate = endDate,
                    Approved = null,
                    DateRequested = DateTime.Now,
                    DateActioned = DateTime.Now,
                    LeaveTypeId = model.LeaveTypeId,
                    Cancelled = false,
                    ReasonForRequest = model.ReasonForRequest
                };

                var leaveRequest = _mapper.Map<LeaveRequest>(leaveRequestModel);
                var isSuccess = await _leaveRequestRepo.Create(leaveRequest);

                if(!isSuccess)
                {
                    ModelState.AddModelError("", "Something went wrong with submitting your record.");
                    return View(model);
                }

                return RedirectToAction(nameof(MyLeave));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Something went wrong.");
                return View(model);
            }
        }

        public async Task<ActionResult> MyLeave()
        {
            try
            {
                var employee = await _userManager.GetUserAsync(User);
                var employeeId = employee.Id;

                var leaveAllocations = await _leaveAllocationRepo.GetLeaveAllocationsByEmployee(employeeId);
                var leaveRequests = await _leaveRequestRepo.GetLeaveRequestsByEmployee(employeeId);

                var model = new EmployeeLeaveRequestViewVM
                {
                    LeaveAllocations = _mapper.Map<List<LeaveAllocationVM>>(leaveAllocations),
                    LeaveRequests = _mapper.Map<List<LeaveRequestVM>>(leaveRequests)
                };
                return View(model);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<ActionResult> CancelRequest(int id)
        {
            try
            {
                var leaveRequest = await _leaveRequestRepo.FindById(id);
                leaveRequest.Cancelled = true;

                await _leaveRequestRepo.Update(leaveRequest);

                return RedirectToAction(nameof(MyLeave));
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(MyLeave));
            }
        }
    }
}