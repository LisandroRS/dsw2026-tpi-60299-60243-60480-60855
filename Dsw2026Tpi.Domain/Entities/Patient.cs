using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities;

public class Patient : EntityBase
{
    public string Email { get; private set; }
    public long Dni { get; private set; }
    public string FullName { get; private set; }
    public string IdentityUserId { get; private set; }

    #region Constructor for EF
#pragma warning disable CS8618
    private Patient()
    {
    }
#pragma warning restore CS8618
    #endregion

    public Patient(string email, long dni, string identityUserId, Guid? id = null) : base(id)
    {
        Email = email;
        Dni = dni;
        FullName = string.Empty;
        IdentityUserId = identityUserId;
    }
}