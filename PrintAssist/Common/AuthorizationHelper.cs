using System;
using System.IO;
using System.Security.AccessControl;

namespace PrintAssist.Common
{
    /// <summary>
    /// 系统路径和文件授权辅助类
    /// </summary>
    public class AuthorizationHelper
    {
        /// <summary>
        /// 为文件添加users，everyone用户组的完全控制权限
        /// </summary>
        /// <param name="filePath"></param>
        public static void AddSecurityControll2File(string filePath)
        {
            try
            {
                //获取文件信息
                FileInfo fileInfo = new FileInfo(filePath);
                //获得该文件的访问权限
                System.Security.AccessControl.FileSecurity fileSecurity = fileInfo.GetAccessControl();
                //添加ereryone用户组的访问权限规则 完全控制权限
                fileSecurity.AddAccessRule(new FileSystemAccessRule("Everyone", FileSystemRights.FullControl, AccessControlType.Allow));
                //添加Users用户组的访问权限规则 完全控制权限
                fileSecurity.AddAccessRule(new FileSystemAccessRule("Users", FileSystemRights.FullControl, AccessControlType.Allow));
                //设置访问权限
                fileInfo.SetAccessControl(fileSecurity);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Console.WriteLine("AddSecurityControll2File is executed.");
            }
        }

        /// <summary>
        ///为文件夹添加users，everyone用户组的完全控制权限
        /// </summary>
        /// <param name="dirPath"></param>
        public static void AddSecurityControll2Folder(string dirPath)
        {
            try
            {
                //获取文件夹信息
                DirectoryInfo dir = new DirectoryInfo(dirPath);
                //获得该文件夹的所有访问权限
                System.Security.AccessControl.DirectorySecurity dirSecurity = dir.GetAccessControl(AccessControlSections.All);
                //设定文件ACL继承
                InheritanceFlags inherits = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
                //添加ereryone用户组的访问权限规则 完全控制权限
                FileSystemAccessRule everyoneFileSystemAccessRule = new FileSystemAccessRule("Everyone", FileSystemRights.FullControl, inherits, PropagationFlags.None, AccessControlType.Allow);
                //添加Users用户组的访问权限规则 完全控制权限
                FileSystemAccessRule usersFileSystemAccessRule = new FileSystemAccessRule("Users", FileSystemRights.FullControl, inherits, PropagationFlags.None, AccessControlType.Allow);
                bool isModified = false;
                dirSecurity.ModifyAccessRule(AccessControlModification.Add, everyoneFileSystemAccessRule, out isModified);
                dirSecurity.ModifyAccessRule(AccessControlModification.Add, usersFileSystemAccessRule, out isModified);
                //设置访问权限
                dir.SetAccessControl(dirSecurity);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Console.WriteLine("AddSecurityControll2Folder is executed.");
            }
        }
    }
}
