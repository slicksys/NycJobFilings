using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NycJobFilings.Data.Models
{
    /// <summary>
    /// Represents a DOB Job Application Filing from NYC Open Data
    /// </summary>
    public class JobFiling
    {
        /// <summary>
        /// Primary key - Job S1 Number
        /// </summary>
        [Key]
        [Column("JOB_S1_NO")]
        public string JobS1No { get; set; } = default!;

        [Column("BOROUGH")]
        public string? Borough { get; set; }

        [Column("HOUSE_NO")]
        public string? HouseNo { get; set; }

        [Column("STREET_NAME")]
        public string? StreetName { get; set; }

        [Column("BLOCK")]
        public string? Block { get; set; }

        [Column("LOT")]
        public string? Lot { get; set; }

        [Column("ZIP_CODE")]
        public string? ZipCode { get; set; }

        [Column("BLDG_TYPE")]
        public string? BuildingType { get; set; }

        [Column("JOB_TYPE")]
        public string? JobType { get; set; }

        [Column("JOB_STATUS")]
        public string? JobStatus { get; set; }

        [Column("JOB_STATUS_DESCRP")]
        public string? JobStatusDescription { get; set; }

        [Column("LATEST_ACTION_DATE")]
        public DateTime? LatestActionDate { get; set; }

        [Column("FILING_DATE")]
        public DateTime? FilingDate { get; set; }

        [Column("APPROVED_DATE")]
        public DateTime? ApprovedDate { get; set; }

        [Column("FULLY_PAID")]
        public string? FullyPaid { get; set; }

        [Column("INITIAL_COST", TypeName = "decimal(18,2)")]
        public decimal? InitialCost { get; set; }

        [Column("TOTAL_EST_FEE", TypeName = "decimal(18,2)")]
        public decimal? TotalEstimatedFee { get; set; }

        [Column("FEE_STATUS")]
        public string? FeeStatus { get; set; }

        [Column("EXISTING_DWELLING_UNITS")]
        public int? ExistingDwellingUnits { get; set; }

        [Column("PROPOSED_DWELLING_UNITS")]
        public int? ProposedDwellingUnits { get; set; }

        [Column("EXISTING_OCCUPANCY")]
        public string? ExistingOccupancy { get; set; }

        [Column("PROPOSED_OCCUPANCY")]
        public string? ProposedOccupancy { get; set; }

        [Column("EXISTING_STORIES")]
        public int? ExistingStories { get; set; }

        [Column("PROPOSED_STORIES")]
        public int? ProposedStories { get; set; }

        [Column("EXISTING_ZONING_SQFT", TypeName = "decimal(18,2)")]
        public decimal? ExistingZoningSquareFeet { get; set; }

        [Column("PROPOSED_ZONING_SQFT", TypeName = "decimal(18,2)")]
        public decimal? ProposedZoningSquareFeet { get; set; }

        [Column("HORIZONTAL_ENLRGMT", TypeName = "decimal(18,2)")]
        public decimal? HorizontalEnlargement { get; set; }

        [Column("VERTICAL_ENLRGMT", TypeName = "decimal(18,2)")]
        public decimal? VerticalEnlargement { get; set; }

        [Column("ENLARGEMENT_SQFT", TypeName = "decimal(18,2)")]
        public decimal? EnlargementSquareFeet { get; set; }

        [Column("OWNER_TYPE")]
        public string? OwnerType { get; set; }

        [Column("OWNER_NAME")]
        public string? OwnerName { get; set; }

        [Column("OWNER_BUSINESS")]
        public string? OwnerBusiness { get; set; }

        [Column("OWNER_HOUSE_STREET")]
        public string? OwnerHouseStreet { get; set; }

        [Column("CITY_STATE_ZIP")]
        public string? CityStateZip { get; set; }
    }
}
