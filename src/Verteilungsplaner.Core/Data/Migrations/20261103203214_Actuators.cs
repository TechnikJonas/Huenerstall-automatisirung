using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Verteilungsplaner.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class Actuators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActuatorTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Mounting = table.Column<int>(type: "INTEGER", nullable: false),
                    ChannelCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxCurrentPerChannel = table.Column<double>(type: "REAL", nullable: false),
                    MaxPowerPerChannel = table.Column<double>(type: "REAL", nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    ArticleNumber = table.Column<string>(type: "TEXT", nullable: true),
                    WidthTe = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActuatorTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "Actuators",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    TemplateId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DistributionBoardId = table.Column<int>(type: "INTEGER", nullable: true),
                    RoomId = table.Column<int>(type: "INTEGER", nullable: true),
                    Remark = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actuators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Actuators_ActuatorTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "ActuatorTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Actuators_DistributionBoards_DistributionBoardId",
                        column: x => x.DistributionBoardId,
                        principalTable: "DistributionBoards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Actuators_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Actuators_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RoomFunctions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoomId = table.Column<int>(type: "INTEGER", nullable: false),
                    FunctionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Circuit = table.Column<int>(type: "INTEGER", nullable: false),
                    Designation = table.Column<string>(type: "TEXT", nullable: true),
                    LoadCurrent = table.Column<double>(type: "REAL", nullable: true),
                    LoadPower = table.Column<double>(type: "REAL", nullable: true),
                    ActuatorId = table.Column<int>(type: "INTEGER", nullable: true),
                    Channel = table.Column<int>(type: "INTEGER", nullable: true),
                    Remark = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomFunctions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomFunctions_Actuators_ActuatorId",
                        column: x => x.ActuatorId,
                        principalTable: "Actuators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RoomFunctions_CableFunctions_FunctionId",
                        column: x => x.FunctionId,
                        principalTable: "CableFunctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoomFunctions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoomFunctions_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Actuators_DistributionBoardId",
                table: "Actuators",
                column: "DistributionBoardId");

            migrationBuilder.CreateIndex(
                name: "IX_Actuators_ProjectId",
                table: "Actuators",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Actuators_RoomId",
                table: "Actuators",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Actuators_TemplateId",
                table: "Actuators",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomFunctions_ActuatorId",
                table: "RoomFunctions",
                column: "ActuatorId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomFunctions_FunctionId",
                table: "RoomFunctions",
                column: "FunctionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomFunctions_ProjectId",
                table: "RoomFunctions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomFunctions_RoomId",
                table: "RoomFunctions",
                column: "RoomId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "RoomFunctions");

            migrationBuilder.DropTable(
                name: "Actuators");

            migrationBuilder.DropTable(
                name: "ActuatorTemplates");
        }
    }
}
