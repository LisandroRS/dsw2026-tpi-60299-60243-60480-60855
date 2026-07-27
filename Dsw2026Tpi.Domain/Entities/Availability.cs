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
    public SlotStatus Status { get; private set; }
    public bool Deleted { get; private set; }

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
        Status = SlotStatus.Available;
        Deleted = false;
    }

    public bool IsAvailable => Status == SlotStatus.Available && !Deleted;

    public void Book()
    {
        if (!IsAvailable)
            throw new InvalidOperationException($"El turno {Id} no está disponible para reservar.");

        Status = SlotStatus.Booked;
    }

    public void Release()
    {
        if (Deleted)
            throw new InvalidOperationException($"El turno {Id} fue dado de baja.");

        Status = SlotStatus.Available;
    }

    public void Block()
    {
        if (Deleted)
            throw new InvalidOperationException($"El turno {Id} fue dado de baja.");

        Status = SlotStatus.Blocked;
    }

    public void Delete()
    {
        Deleted = true;
    }
}
