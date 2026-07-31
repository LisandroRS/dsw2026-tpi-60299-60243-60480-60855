using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    public class Turn : EntityBase
    {
        public Guid DoctorId { get; private set; }
        public Doctor? Doctor { get; private set; }

        public Guid AvailabilityId { get; private set; }
        public Availability? Availability { get; private set; }

        public DateOnly Date { get; private set; }
        public TimeOnly StartTime { get; private set; }
        public TimeOnly EndTime { get; private set; }

        public TurnStatus Status { get; private set; }
        public bool Deleted { get; private set; }

        #region Constructor for EF
#pragma warning disable CS8618
        private Turn()
        {
        }
#pragma warning restore CS8618
        #endregion

        public Turn(
            Availability availability,
            DateOnly date,
            TimeOnly startTime,
            TimeOnly endTime,
            Guid? id = null) : base(id)
        {
            Availability = availability;
            AvailabilityId = availability.Id;
            DoctorId = availability.DoctorId;      
            Date = date;
            StartTime = startTime;
            EndTime = endTime;
            Status = TurnStatus.Available;
            Deleted = false;
        }

        
        public bool IsAvailable => Status == TurnStatus.Available && !Deleted;

        public DateTime StartDateTime => Date.ToDateTime(StartTime);

        public void Book()
        {
            if (!IsAvailable)
                throw new InvalidOperationException(
                    $"El turno {Id} no está disponible para reservar (estado: {Status}).");

            Status = TurnStatus.Booked;
        }

        public void Release()
        {
            if (Deleted)
                throw new InvalidOperationException($"El turno {Id} fue dado de baja.");

            if (Status != TurnStatus.Booked)
                throw new InvalidOperationException(
                    $"El turno {Id} no está reservado (estado: {Status}).");

            Status = TurnStatus.Available;
        }

        public void Block()
        {
            if (Deleted)
                throw new InvalidOperationException($"El turno {Id} fue dado de baja.");

            if (Status == TurnStatus.Booked)
                throw new InvalidOperationException(
                    $"El turno {Id} está reservado, no se puede bloquear.");

            Status = TurnStatus.Blocked;
        }

        public void Delete()
        {
            Deleted = true;
        }
    }
}
