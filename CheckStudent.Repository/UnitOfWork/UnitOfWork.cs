using CheckStudent.Repository.Models;
using CheckStudent.Repository.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckStudent.Repository.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private CheckStudentContext _context;
        private GenericRepository<Student> _studentRepo;
        private GenericRepository<StudentFace> _studentFaceRepo;
        private GenericRepository<Course> _courseRepo;


        public UnitOfWork(CheckStudentContext context)
        {
            _context = context;
        }




        public void Save()
        {
            _context.SaveChanges();
        }
        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }
        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        GenericRepository<Student> IUnitOfWork.StudentRepository
        {
            get
            {
                if (_studentRepo == null)
                {
                    this._studentRepo = new GenericRepository<Student>(_context);
                }
                return _studentRepo;
            }
        }

        GenericRepository<StudentFace> IUnitOfWork.StudentFaceRepository
        {
            get
            {
                if (_studentFaceRepo == null)
                {
                    this._studentFaceRepo = new GenericRepository<StudentFace>(_context);
                }
                return _studentFaceRepo;
            }
        }

        public GenericRepository<Course> OrderRepository
        {
            get
            {
                if (_courseRepo == null)
                {
                    this._courseRepo = new GenericRepository<Course>(_context);
                }
                return _courseRepo;
            }
        }

        public GenericRepository<Course> CourseRepository => throw new NotImplementedException();
    }
}
