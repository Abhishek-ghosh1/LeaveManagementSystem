using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leave_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class dfvbg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeaveMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaveName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDatetime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedDatetime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDatetime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedDatetime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Team_Project",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDatetime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedDatetime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Team_Project", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PinNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Team_ProjectId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalNoofLeaves = table.Column<int>(type: "int", nullable: true),
                    UsedLeaves = table.Column<int>(type: "int", nullable: true),
                    LeftLeaves = table.Column<int>(type: "int", nullable: true),
                    LeaveSpendingForApproval = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDatetime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedDatetime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeaveMatrix",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleMasterId = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    ToRole = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDatetime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedDatetime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveMatrix", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveMatrix_RoleMaster_RoleMasterId",
                        column: x => x.RoleMasterId,
                        principalTable: "RoleMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LeaveMatrix_RoleMaster_ToRole",
                        column: x => x.ToRole,
                        principalTable: "RoleMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Leave",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferenceNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDraft = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sl = table.Column<int>(type: "int", nullable: true),
                    EmpUserID = table.Column<int>(type: "int", nullable: false),
                    LeaveReasonId = table.Column<int>(type: "int", nullable: false),
                    LeaveDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JoinDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadPDFFile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OtherRelatedDocs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDatetime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedDatetime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leave", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leave_LeaveMaster_LeaveReasonId",
                        column: x => x.LeaveReasonId,
                        principalTable: "LeaveMaster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Leave_Users_EmpUserID",
                        column: x => x.EmpUserID,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeaveObservationDesiredFlow",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaveId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    RoleId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveObservationDesiredFlow", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveObservationDesiredFlow_Leave_LeaveId",
                        column: x => x.LeaveId,
                        principalTable: "Leave",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeaveObservationDesiredFlow_RoleMaster_RoleId",
                        column: x => x.RoleId,
                        principalTable: "RoleMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LeaveObservationDesiredFlow_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LeaveObservationFlow",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaveId = table.Column<int>(type: "int", nullable: false),
                    LeaveObservationDesiredFlowId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    TargetDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActionTakenDatetime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status_to = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionTaken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Document = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedDatetime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    MannualEntry = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveObservationFlow", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveObservationFlow_LeaveObservationDesiredFlow_LeaveObservationDesiredFlowId",
                        column: x => x.LeaveObservationDesiredFlowId,
                        principalTable: "LeaveObservationDesiredFlow",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LeaveObservationFlow_Leave_LeaveId",
                        column: x => x.LeaveId,
                        principalTable: "Leave",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LeaveObservationFlow_RoleMaster_RoleId",
                        column: x => x.RoleId,
                        principalTable: "RoleMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LeaveObservationFlow_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Leave_EmpUserID",
                table: "Leave",
                column: "EmpUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Leave_LeaveReasonId",
                table: "Leave",
                column: "LeaveReasonId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveMatrix_RoleMasterId",
                table: "LeaveMatrix",
                column: "RoleMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveMatrix_ToRole",
                table: "LeaveMatrix",
                column: "ToRole");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveObservationDesiredFlow_LeaveId",
                table: "LeaveObservationDesiredFlow",
                column: "LeaveId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveObservationDesiredFlow_RoleId",
                table: "LeaveObservationDesiredFlow",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveObservationDesiredFlow_UserId",
                table: "LeaveObservationDesiredFlow",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveObservationFlow_LeaveId",
                table: "LeaveObservationFlow",
                column: "LeaveId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveObservationFlow_LeaveObservationDesiredFlowId",
                table: "LeaveObservationFlow",
                column: "LeaveObservationDesiredFlowId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveObservationFlow_RoleId",
                table: "LeaveObservationFlow",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveObservationFlow_UserId",
                table: "LeaveObservationFlow",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaveMatrix");

            migrationBuilder.DropTable(
                name: "LeaveObservationFlow");

            migrationBuilder.DropTable(
                name: "Team_Project");

            migrationBuilder.DropTable(
                name: "LeaveObservationDesiredFlow");

            migrationBuilder.DropTable(
                name: "Leave");

            migrationBuilder.DropTable(
                name: "RoleMaster");

            migrationBuilder.DropTable(
                name: "LeaveMaster");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
