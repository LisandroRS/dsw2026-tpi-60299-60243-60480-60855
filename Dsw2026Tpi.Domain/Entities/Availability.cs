using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities;

public class Availability : EntityBase
{
    public Guid DoctorId { get; private set; }
    public Doctor? Doctor { get; private set; }

    public int Year { get; private set; }
    public int Month { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }

    public bool Deleted { get; private set; }

    public ICollection<Turn> Turns { get; private set; } = new List<Turn>();

    #region Constructor for EF
#pragma warning disable CS8618
    private Availability()
    {
    }
#pragma warning restore CS8618
    #endregion

    public Availability(
        Doctor doctor,
        int year,
        int month,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid? id = null) : base(id)
    {
        Doctor = doctor;
        DoctorId = doctor.Id;
        Year = year;
        Month = month;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        Deleted = false;
    }

    public void Delete()
    {
        Deleted = true;
    }
}
