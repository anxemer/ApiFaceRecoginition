﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace CheckStudent.Repository.Models;

public partial class StudentFace
{
    public int Id { get; set; }

    public byte[] FaceData { get; set; }

    public DateTime? CaptureDate { get; set; }

    public int StudentId { get; set; }

    public virtual Student Student { get; set; }
}