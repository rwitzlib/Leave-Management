﻿using Leave_Management.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Leave_Management.Contracts
{
    public interface ILeaveAllocationRepository : IRepositoryBase<LeaveAllocation>
    {
        bool CheckAllocation(int leavetypeid, string emplyeeid);
        ICollection<LeaveAllocation> GetLeaveAllocationsByEmployee(string employeeId);
        LeaveAllocation GetLeaveAllocationsByEmployeeAndType(string employeeId, int leaveTypeId);

    }
}
