using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    public class Appointment : EntityBase
    {
        public Guid AvailabilityId { get; private set; }
        public Availability? Availability { get; private set; }

        public Guid PatientId { get; private set; }
        public Patient? Patient { get; private set; }

        public string Reason { get; private set; }
        public AppointmentStatus Status { get; private set; }

        public DateTime? CancelledAt { get; private set; }
        public DateTime? AttendedAt { get; private set; }

        public byte[] RowVersion { get; private set; } //para no tener concurrencia lo que hacemos es que el row version hace que sql se fije la version de la fila
                                                       // si las versiones de row version no son iguales no se hace el update
        #region Constructor for EF
#pragma warning disable CS8618
        private Appointment()
        {
        }
#pragma warning restore CS8618
        #endregion

        public Appointment(Availability availability, Patient patient, string reason, Guid? id = null) : base(id)
        {
            Availability = availability;
            AvailabilityId = availability.Id;
            Patient = patient;
            PatientId = patient.Id;
            Reason = reason;
            Status = AppointmentStatus.Booked;
        }

    
        public bool IsActive => Status == AppointmentStatus.Booked;

        public void Cancel()
        {
            EnsureIsActive("cancelar");

            Status = AppointmentStatus.Cancelled;
            CancelledAt = DateTime.Now;
        }

        public void MarkAsAttended()
        {
            EnsureIsActive("marcar como atendida");

            Status = AppointmentStatus.Attended;
            AttendedAt = DateTime.Now;
        }

        public void MarkAsNoShow()
        {
            EnsureIsActive("marcar como ausente");

            Status = AppointmentStatus.NoShow;
        }

        private void EnsureIsActive(string action)
        {
            if (!IsActive)
                throw new InvalidOperationException(
                    $"No se puede {action} la cita {Id}: su estado es {Status} y solo se opera sobre citas en estado {AppointmentStatus.Booked}.");
        }
    }
}
