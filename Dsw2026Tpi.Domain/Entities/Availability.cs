using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities;

public class Availability : EntityBase
{
    public Guid DoctorId { get; private set; }
    public Doctor? Doctor { get; private set; }
    public DateTime StartDateTime { get; private set; }
    public DateTime EndDateTime { get; private set; }
    public bool IsAvailable { get; private set; }

    #region Constructor for EF
#pragma warning disable CS8618
    private Availability()
    {
    }
#pragma warning restore CS8618
    #endregion

    public Availability(Doctor doctor, DateTime startDateTime, DateTime endDateTime, Guid? id = null) : base(id)
    {
        Doctor = doctor;
        DoctorId = doctor.Id;
        StartDateTime = startDateTime;
        EndDateTime = endDateTime;
        IsAvailable = true;
    }
}
