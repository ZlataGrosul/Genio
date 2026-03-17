using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;

namespace Genio
{
    public partial class GenioAppEntities : DbContext
    {
        public GenioAppEntities()
            : base("name=GenioAppEntities")
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }

        public virtual DbSet<EventType> EventTypes { get; set; }
        public virtual DbSet<HonorBoard> HonorBoards { get; set; }
        public virtual DbSet<Olimp> Olimps { get; set; }
        public virtual DbSet<Specialization> Specializations { get; set; }
        public virtual DbSet<StudentOlimp> StudentOlimps { get; set; }
        public virtual DbSet<Student> Students { get; set; }

        
        // СПЕЦИАЛЬНОСТИ
        
        public List<Specialization> Specializations_GetAll()
        {
            return this.Database.SqlQuery<Specialization>(
                "EXEC usp_Specializations_GetAll").ToList();
        }

        public Specialization Specializations_GetById(int specialization_id)
        {
            return this.Database.SqlQuery<Specialization>(
                "EXEC usp_Specializations_GetById @specialization_id = @p0", specialization_id).FirstOrDefault();
        }

        public int Specializations_Insert(string spec_name)
        {
            var outputParam = new SqlParameter("@specialization_id", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            this.Database.ExecuteSqlCommand(
                "EXEC usp_Specializations_Insert @spec_name = @spec_name, @specialization_id = @specialization_id OUTPUT",
                new SqlParameter("@spec_name", spec_name),
                outputParam);

            return (int)outputParam.Value;
        }

        public void Specializations_Update(int specialization_id, string spec_name)
        {
            this.Database.ExecuteSqlCommand(
                "EXEC usp_Specializations_Update @specialization_id = @specialization_id, @spec_name = @spec_name",
                new SqlParameter("@specialization_id", specialization_id),
                new SqlParameter("@spec_name", spec_name));
        }

        public void Specializations_Delete(int specialization_id)
        {
            this.Database.ExecuteSqlCommand(
                "EXEC usp_Specializations_Delete @specialization_id = @specialization_id",
                new SqlParameter("@specialization_id", specialization_id));
        }

        
        // ВИДЫ МЕРОПРИЯТИЙ
        
        public List<EventType> EventTypes_GetAll()
        {
            return this.Database.SqlQuery<EventType>(
                "EXEC usp_EventTypes_GetAll").ToList();
        }

        public EventType EventTypes_GetById(int event_type_id)
        {
            return this.Database.SqlQuery<EventType>(
                "EXEC usp_EventTypes_GetById @event_type_id = @p0", event_type_id).FirstOrDefault();
        }

        public int EventTypes_Insert(string type_name)
        {
            var outputParam = new SqlParameter("@event_type_id", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            this.Database.ExecuteSqlCommand(
                "EXEC usp_EventTypes_Insert @type_name = @type_name, @event_type_id = @event_type_id OUTPUT",
                new SqlParameter("@type_name", type_name),
                outputParam);

            return (int)outputParam.Value;
        }

        public void EventTypes_Update(int event_type_id, string type_name)
        {
            this.Database.ExecuteSqlCommand(
                "EXEC usp_EventTypes_Update @event_type_id = @event_type_id, @type_name = @type_name",
                new SqlParameter("@event_type_id", event_type_id),
                new SqlParameter("@type_name", type_name));
        }

        public void EventTypes_Delete(int event_type_id)
        {
            this.Database.ExecuteSqlCommand(
                "EXEC usp_EventTypes_Delete @event_type_id = @event_type_id",
                new SqlParameter("@event_type_id", event_type_id));
        }

        
        // СТУДЕНТЫ
        
        public List<Student> Students_GetAll()
        {
            return this.Database.SqlQuery<Student>(
                "EXEC usp_Students_GetAll").ToList();
        }

        public Student Students_GetById(int student_id)
        {
            return this.Database.SqlQuery<Student>(
                "EXEC usp_Students_GetById @student_id = @p0", student_id).FirstOrDefault();
        }

        public int Students_Insert(string last_name, string first_name, string middle_name,
            DateTime birth_date, string phone, string home_phone, DateTime admission_date,
            DateTime? graduation_date, int course_number, string group_name, int specialization_id)
        {
            var outputParam = new SqlParameter("@student_id", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            this.Database.ExecuteSqlCommand(
                "EXEC usp_Students_Insert @last_name = @last_name, @first_name = @first_name, " +
                "@middle_name = @middle_name, @birth_date = @birth_date, @phone = @phone, " +
                "@home_phone = @home_phone, @admission_date = @admission_date, " +
                "@graduation_date = @graduation_date, @course_number = @course_number, " +
                "@group_name = @group_name, @specialization_id = @specialization_id, " +
                "@student_id = @student_id OUTPUT",
                new SqlParameter("@last_name", last_name),
                new SqlParameter("@first_name", first_name),
                new SqlParameter("@middle_name", (object)middle_name ?? DBNull.Value),
                new SqlParameter("@birth_date", birth_date),
                new SqlParameter("@phone", (object)phone ?? DBNull.Value),
                new SqlParameter("@home_phone", (object)home_phone ?? DBNull.Value),
                new SqlParameter("@admission_date", admission_date),
                new SqlParameter("@graduation_date", (object)graduation_date ?? DBNull.Value),
                new SqlParameter("@course_number", course_number),
                new SqlParameter("@group_name", group_name),
                new SqlParameter("@specialization_id", specialization_id),
                outputParam);

            return (int)outputParam.Value;
        }

        public void Students_Update(int student_id, string last_name, string first_name,
            string middle_name, DateTime birth_date, string phone, string home_phone,
            DateTime admission_date, DateTime? graduation_date, int course_number,
            string group_name, int specialization_id)
        {
            this.Database.ExecuteSqlCommand(
                "EXEC usp_Students_Update @student_id = @student_id, @last_name = @last_name, " +
                "@first_name = @first_name, @middle_name = @middle_name, @birth_date = @birth_date, " +
                "@phone = @phone, @home_phone = @home_phone, @admission_date = @admission_date, " +
                "@graduation_date = @graduation_date, @course_number = @course_number, " +
                "@group_name = @group_name, @specialization_id = @specialization_id",
                new SqlParameter("@student_id", student_id),
                new SqlParameter("@last_name", last_name),
                new SqlParameter("@first_name", first_name),
                new SqlParameter("@middle_name", (object)middle_name ?? DBNull.Value),
                new SqlParameter("@birth_date", birth_date),
                new SqlParameter("@phone", (object)phone ?? DBNull.Value),
                new SqlParameter("@home_phone", (object)home_phone ?? DBNull.Value),
                new SqlParameter("@admission_date", admission_date),
                new SqlParameter("@graduation_date", (object)graduation_date ?? DBNull.Value),
                new SqlParameter("@course_number", course_number),
                new SqlParameter("@group_name", group_name),
                new SqlParameter("@specialization_id", specialization_id));
        }

        public void Students_Delete(int student_id)
        {
            this.Database.ExecuteSqlCommand(
                "EXEC usp_Students_Delete @student_id = @student_id",
                new SqlParameter("@student_id", student_id));
        }

        
        // ОЛИМПИАДЫ
        
        public List<Olimp> Olimps_GetAll()
        {
            return this.Database.SqlQuery<Olimp>(
                "EXEC usp_Olimps_GetAll").ToList();
        }

        public Olimp Olimps_GetById(int olimp_id)
        {
            return this.Database.SqlQuery<Olimp>(
                "EXEC usp_Olimps_GetById @olimp_id = @p0", olimp_id).FirstOrDefault();
        }

        public int Olimps_Insert(string olimp_name, DateTime olimp_date, int event_type_id,
            string olimp_level, string olimp_location, string nominations)
        {
            var outputParam = new SqlParameter("@olimp_id", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            this.Database.ExecuteSqlCommand(
                "EXEC usp_Olimps_Insert @olimp_name = @olimp_name, @olimp_date = @olimp_date, " +
                "@event_type_id = @event_type_id, @olimp_level = @olimp_level, " +
                "@olimp_location = @olimp_location, @nominations = @nominations, " +
                "@olimp_id = @olimp_id OUTPUT",
                new SqlParameter("@olimp_name", olimp_name),
                new SqlParameter("@olimp_date", olimp_date),
                new SqlParameter("@event_type_id", event_type_id),
                new SqlParameter("@olimp_level", (object)olimp_level ?? DBNull.Value),
                new SqlParameter("@olimp_location", (object)olimp_location ?? DBNull.Value),
                new SqlParameter("@nominations", (object)nominations ?? DBNull.Value),
                outputParam);

            return (int)outputParam.Value;
        }

        public void Olimps_Update(int olimp_id, string olimp_name, DateTime olimp_date,
            int event_type_id, string olimp_level, string olimp_location, string nominations)
        {
            this.Database.ExecuteSqlCommand(
                "EXEC usp_Olimps_Update @olimp_id = @olimp_id, @olimp_name = @olimp_name, " +
                "@olimp_date = @olimp_date, @event_type_id = @event_type_id, " +
                "@olimp_level = @olimp_level, @olimp_location = @olimp_location, " +
                "@nominations = @nominations",
                new SqlParameter("@olimp_id", olimp_id),
                new SqlParameter("@olimp_name", olimp_name),
                new SqlParameter("@olimp_date", olimp_date),
                new SqlParameter("@event_type_id", event_type_id),
                new SqlParameter("@olimp_level", (object)olimp_level ?? DBNull.Value),
                new SqlParameter("@olimp_location", (object)olimp_location ?? DBNull.Value),
                new SqlParameter("@nominations", (object)nominations ?? DBNull.Value));
        }

        public void Olimps_Delete(int olimp_id)
        {
            this.Database.ExecuteSqlCommand(
                "EXEC usp_Olimps_Delete @olimp_id = @olimp_id",
                new SqlParameter("@olimp_id", olimp_id));
        }

        
        // УЧАСТИЕ СТУДЕНТОВ
        
        public List<StudentOlimp> StudentOlimps_GetAll()
        {
            return this.Database.SqlQuery<StudentOlimp>(
                "EXEC usp_StudentOlimps_GetAll").ToList();
        }

        public List<StudentOlimp> StudentOlimps_GetByOlimpId(int olimp_id)
        {
            return this.Database.SqlQuery<StudentOlimp>(
                "EXEC usp_StudentOlimps_GetByOlimpId @olimp_id = @p0", olimp_id).ToList();
        }

        public List<StudentOlimp> StudentOlimps_GetByStudentId(int student_id)
        {
            return this.Database.SqlQuery<StudentOlimp>(
                "EXEC usp_StudentOlimps_GetByStudentId @student_id = @p0", student_id).ToList();
        }

        public int StudentOlimps_Insert(int student_id, int olimp_id, string result)
        {
            var outputParam = new SqlParameter("@participation_id", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            this.Database.ExecuteSqlCommand(
                "EXEC usp_StudentOlimps_Insert @student_id = @student_id, @olimp_id = @olimp_id, " +
                "@result = @result, @participation_id = @participation_id OUTPUT",
                new SqlParameter("@student_id", student_id),
                new SqlParameter("@olimp_id", olimp_id),
                new SqlParameter("@result", result),
                outputParam);

            return (int)outputParam.Value;
        }

        public void StudentOlimps_Update(int participation_id, string result)
        {
            this.Database.ExecuteSqlCommand(
                "EXEC usp_StudentOlimps_Update @participation_id = @participation_id, @result = @result",
                new SqlParameter("@participation_id", participation_id),
                new SqlParameter("@result", result));
        }

        public void StudentOlimps_Delete(int participation_id)
        {
            this.Database.ExecuteSqlCommand(
                "EXEC usp_StudentOlimps_Delete @participation_id = @participation_id",
                new SqlParameter("@participation_id", participation_id));
        }

        public void StudentOlimps_DeleteByOlimpId(int olimp_id)
        {
            this.Database.ExecuteSqlCommand(
                "EXEC usp_StudentOlimps_DeleteByOlimpId @olimp_id = @olimp_id",
                new SqlParameter("@olimp_id", olimp_id));
        }

        
        // ДОСКА ПОЧЕТА
        
        public List<HonorBoard> HonorBoard_GetAll()
        {
            return this.Database.SqlQuery<HonorBoard>(
                "EXEC usp_HonorBoard_GetAll").ToList();
        }

        public HonorBoard HonorBoard_GetById(int honor_id)
        {
            return this.Database.SqlQuery<HonorBoard>(
                "EXEC usp_HonorBoard_GetById @honor_id = @p0", honor_id).FirstOrDefault();
        }

        public List<HonorBoard> HonorBoard_GetByDate(DateTime placement_date)
        {
            return this.Database.SqlQuery<HonorBoard>(
                "EXEC usp_HonorBoard_GetByDate @placement_date = @p0", placement_date).ToList();
        }

        public int HonorBoard_Insert(int student_id, DateTime placement_date)
        {
            var outputParam = new SqlParameter("@honor_id", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            this.Database.ExecuteSqlCommand(
                "EXEC usp_HonorBoard_Insert @student_id = @student_id, @placement_date = @placement_date, " +
                "@honor_id = @honor_id OUTPUT",
                new SqlParameter("@student_id", student_id),
                new SqlParameter("@placement_date", placement_date),
                outputParam);

            return (int)outputParam.Value;
        }

        public void HonorBoard_Update(int honor_id, int student_id, DateTime placement_date)
        {
            this.Database.ExecuteSqlCommand(
                "EXEC usp_HonorBoard_Update @honor_id = @honor_id, @student_id = @student_id, " +
                "@placement_date = @placement_date",
                new SqlParameter("@honor_id", honor_id),
                new SqlParameter("@student_id", student_id),
                new SqlParameter("@placement_date", placement_date));
        }

        public void HonorBoard_Delete(int honor_id)
        {
            this.Database.ExecuteSqlCommand(
                "EXEC usp_HonorBoard_Delete @honor_id = @honor_id",
                new SqlParameter("@honor_id", honor_id));
        }

        public void HonorBoard_DeleteByDate(DateTime placement_date)
        {
            this.Database.ExecuteSqlCommand(
                "EXEC usp_HonorBoard_DeleteByDate @placement_date = @placement_date",
                new SqlParameter("@placement_date", placement_date));
        }

        
        // АНАЛИТИКА
        
        public List<dynamic> Analytics_GetStudentStatistics()
        {
            return this.Database.SqlQuery<dynamic>(
                "EXEC usp_Analytics_GetStudentStatistics").ToList();
        }

        public List<dynamic> Analytics_GetGroupStatistics()
        {
            return this.Database.SqlQuery<dynamic>(
                "EXEC usp_Analytics_GetGroupStatistics").ToList();
        }

        public List<dynamic> Analytics_GetSpecializationStats()
        {
            return this.Database.SqlQuery<dynamic>(
                "EXEC usp_Analytics_GetSpecializationStats").ToList();
        }

        public List<dynamic> Analytics_GetCurrentMonthOlimps()
        {
            return this.Database.SqlQuery<dynamic>(
                "EXEC usp_Analytics_GetCurrentMonthOlimps").ToList();
        }

        public List<dynamic> Analytics_GetOlimpsByPeriod(DateTime startDate, DateTime endDate)
        {
            return this.Database.SqlQuery<dynamic>(
                "EXEC usp_Analytics_GetOlimpsByPeriod @startDate = @p0, @endDate = @p1",
                startDate, endDate).ToList();
        }
    }
}