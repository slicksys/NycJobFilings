using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NycJobFilings.Data.Models
{
    public partial class JobFiling
    {
        [Key]
        [Column("JOB_S1_NO")]
        public int JobS1No { get; set; }

        [Column("Job Description")]
        [StringLength(255)]
        public string? JobDescription { get; set; }

        [Column("Job #")]
        public double? Job { get; set; }

        [Column("Doc #")]
        public double? Doc { get; set; }

        [StringLength(255)]
        public string? Borough { get; set; }

        [Column("House #")]
        [StringLength(255)]
        public string? HouseNo { get; set; }

        [Column("Street Name")]
        [StringLength(255)]
        public string? StreetName { get; set; }

        public double? Block { get; set; }

        public double? Lot { get; set; }

        [Column("City ")]
        [StringLength(255)]
        public string? City { get; set; }

        [StringLength(255)]
        public string? State { get; set; }

        [StringLength(255)]
        public string? Zip { get; set; }

        [Column("Building Type")]
        [StringLength(255)]
        public string? BuildingType { get; set; }
        [Column("Job Type")]
        [StringLength(255)]
        public string? JobType { get; set; }

        [Column("Job Status")]
        [StringLength(255)]
        public string? JobStatus { get; set; }


        [Column("Job Status Descrp")]
        [StringLength(255)]
        public string? JobStatusDescrp { get; set; }

        [Column("Latest Action Date", TypeName = "datetime")]
        public DateTime? LatestActionDate { get; set; }

        [Column("Pre- Filing Date", TypeName = "datetime")]
        public DateTime? PreFilingDate { get; set; }


        //[Column("Filing Date", TypeName = "datetime")]
        //public DateTime? FilingDate { get; set; }


       // [Column(TypeName = "datetime")]
       // public DateTime? ApprovedDate { get; set; }
//

        [Column("Fully Paid", TypeName = "datetime")]
        public DateTime? FullyPaid { get; set; }


        [Column("Initial Cost", TypeName = "money")]
        public decimal? InitialCost { get; set; }


        [Column("Total Est# Fee", TypeName = "money")]
        public decimal? TotalEstFee { get; set; }

        [Column("Fee Status")]
        [StringLength(255)]
        public string? FeeStatus { get; set; }

        [Column("Existing Dwelling Units")]
        [StringLength(255)]
        public string? ExistingDwellingUnits { get; set; }

        [Column("Proposed Dwelling Units")]
        public double? ProposedDwellingUnits { get; set; }

        [Column("Existing Occupancy")]
        [StringLength(255)]
        public string? ExistingOccupancy { get; set; }

        [Column("Proposed Occupancy")]
        [StringLength(255)]
        public string? ProposedOccupancy { get; set; }

        [Column("ExistingNo# of Stories")]
        public double? ExistingNoOfStories { get; set; }

        [Column("Proposed No# of Stories")]
        public double? ProposedNoOfStories { get; set; }


        [Column("Existing Zoning Sqft")]
        public double? ExistingZoningSqft { get; set; }

        [Column("Proposed Zoning Sqft")]
        public double? ProposedZoningSqft { get; set; }

        [Column("Horizontal Enlrgmt")]
        [StringLength(255)]
        public string? HorizontalEnlrgmt { get; set; }

        [Column("Vertical Enlrgmt")]
        [StringLength(255)]
        public string? VerticalEnlrgmt { get; set; }

        [Column("Enlargement SQ Footage")]
        public double? EnlargementSqFootage { get; set; }

        [Column("Owner Type")]
        [StringLength(255)]
        public string? OwnerType { get; set; }

        [Column("Owner's First Name")]
        [StringLength(255)]
        public string? OwnerSFirstName { get; set; }

        [Column("Owner's Last Name")]
        [StringLength(255)]
        public string? OwnerSLastName { get; set; }

        [Column("Owner's Business Name")]
        [StringLength(255)]
        public string? OwnerSBusinessName { get; set; }

        [Column("Owner's House Number")]
        [StringLength(255)]
        public string? OwnerSHouseNumber { get; set; }

        [Column("Owner'sHouse Street Name")]
        [StringLength(255)]
        public string? OwnerSHouseStreetName { get; set; }

        [Column("Owner'sPhone #")]
        public double? OwnerSPhone { get; set; }

    }
}
