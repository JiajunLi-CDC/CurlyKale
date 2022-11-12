using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace CurlyKale
{
    public class CurlyKaleInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "CurlyKale";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("705e1350-9a14-42ce-8b5b-54c3cf10c224");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "";
            }
        }
    }
}
