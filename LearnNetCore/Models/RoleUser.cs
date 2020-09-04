using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace LearnNetCore.Models
{
    [Table("tb_m_RoleUser")]
    public class RoleUser : IdentityUserRole<string>
    {
        public int Id { get; set; }
        public User User { get; set; }
        public Role Role { get; set; }
    }
}
