using Leave_Management.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Leave_Management.Contracts
{
    public interface ILeaveAllocationRepository : IRepositoryBase<LeaveAllocation>
    {
        Task<bool> CheckAllocation(int leavetypeid, string emplyeeid);
        Task<ICollection<LeaveAllocation>> GetLeaveAllocationsByEmployee(string employeeId);
        Task<LeaveAllocation> GetLeaveAllocationsByEmployeeAndType(string employeeId, int leaveTypeId);

    }
}
