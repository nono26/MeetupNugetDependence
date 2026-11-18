using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Acl.Backend.Domain;
public class ExceptionDomain : Exception
{
    public int Id { get; set; }
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
}
