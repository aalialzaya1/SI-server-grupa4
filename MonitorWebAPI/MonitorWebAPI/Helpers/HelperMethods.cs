﻿using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using MonitorWebAPI.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MonitorWebAPI.Helpers
{
    public class HelperMethods
    {

        public GroupHierarchyModel FindHierarchyTree(Group g)
        {
            monitorContext mc = new monitorContext();
            GroupHierarchyModel ghm = new GroupHierarchyModel() { GroupId = g.GroupId, Name = g.Name, SubGroups = new List<GroupHierarchyModel>() };
            findSubgroups(ghm, mc);
            return ghm;
        }

        void findSubgroups(GroupHierarchyModel ghm, monitorContext mc)
        {
            var tempList = mc.Groups.Where(x => x.ParentGroup == ghm.GroupId);
            foreach(var group in tempList)
            {
                ghm.SubGroups.Add(new GroupHierarchyModel { GroupId = group.GroupId, Name = group.Name, SubGroups = new List<GroupHierarchyModel>() });
            }
            foreach (var tempGhm in ghm.SubGroups)
            {
                findSubgroups(tempGhm, mc);
            }
        }

        public GroupHierarchyModel FindHierarchyTreeWithDevices(Group g)
        {
            monitorContext mc = new monitorContext();
            GroupHierarchyModel ghm = new GroupHierarchyModel() { GroupId = g.GroupId, Name = g.Name, SubGroups = new List<GroupHierarchyModel>() };
            findSubgroupsWithDevices(ghm, mc);
            return ghm;
        }

        void findSubgroupsWithDevices(GroupHierarchyModel ghm, monitorContext mc)
        {
            var tempList = mc.Groups.Where(x => x.ParentGroup == ghm.GroupId);
            foreach (var group in tempList)
            {
                ghm.SubGroups.Add(new GroupHierarchyModel { GroupId = group.GroupId, Name = group.Name, SubGroups = new List<GroupHierarchyModel>() });
            }
            foreach (var tempGhm in ghm.SubGroups)
            {
                findSubgroupsWithDevices(tempGhm, mc);
            }
            ghm.Devices = (from dg in mc.DeviceGroups
                           join d in mc.Devices on dg.DeviceId equals d.DeviceId
                           where dg.GroupId == ghm.GroupId
                           select d).ToList();
        }



        // ----------- da li uređaj pripada korisnikovom stablu
        public bool CheckIfDeviceBelongsToUsersTree(VerifyUserModel vu, int deviceId)
        {
            monitorContext mc = new monitorContext();
            string groupName = mc.Groups.Where(x => x.GroupId == vu.groupId).FirstOrDefault().Name;
            bool belongs = false;
            DeviceGroup deviceGroup = mc.DeviceGroups.Where(x => x.DeviceId == deviceId).FirstOrDefault();
            if(deviceGroup == null) {
                throw new NullReferenceException("Device with deviceId doesn't belong to any group!");
            }
            int? deviceGroupId = deviceGroup.GroupId;
            GroupHierarchyModel ghm = new GroupHierarchyModel() { GroupId = vu.groupId, Name = groupName, SubGroups = new List<GroupHierarchyModel>() };
            checkSubgroup(ref belongs, ghm, mc, deviceGroupId);
            return belongs;
        }

        void checkSubgroup(ref bool belongs, GroupHierarchyModel ghm, monitorContext mc, int? deviceGroupId)
        {
            var tempList = mc.Groups.Where(x => x.ParentGroup == ghm.GroupId);
            foreach (var group in tempList)
            {
                if(deviceGroupId==group.GroupId)
                {
                    belongs = true;
                    return;
                }
                ghm.SubGroups.Add(new GroupHierarchyModel { GroupId = group.GroupId, Name = group.Name, SubGroups = new List<GroupHierarchyModel>() });
            }
            if(belongs!=true)
            {
                foreach (var tempGhm in ghm.SubGroups)
                {
                    checkSubgroup(ref belongs, tempGhm, mc, deviceGroupId);
                }
            }
        }
        // ------------


        //------------- Da li grupa pripada korisnikovom stablu
        public bool CheckIfGroupBelongsToUsersTree(VerifyUserModel vu, int? groupId)
        {
            monitorContext mc = new monitorContext();
            string groupName = mc.Groups.Where(x => x.GroupId == vu.groupId).FirstOrDefault().Name;

            Group tempGroup  = mc.Groups.Where(x => x.GroupId == groupId).FirstOrDefault();
            if (tempGroup == null)
            {
                throw new NullReferenceException("Group with that id doesn't exist!");
            }

            bool belongs = vu.groupId==groupId;
            GroupHierarchyModel ghm = new GroupHierarchyModel() { GroupId = vu.groupId, Name = groupName, SubGroups = new List<GroupHierarchyModel>() };
            ifGroupBelongsToTree(ref belongs, ghm, mc, groupId);
            return belongs;
        }

        void ifGroupBelongsToTree(ref bool belongs, GroupHierarchyModel ghm, monitorContext mc, int? groupId)
        {
            if(ghm.GroupId==groupId)
            {
                belongs = true;
                return;
            }
            var tempList = mc.Groups.Where(x => x.ParentGroup == ghm.GroupId);
            foreach (var group in tempList)
            {
                if (groupId == group.GroupId)
                {
                    belongs = true;
                    return;
                }
                ghm.SubGroups.Add(new GroupHierarchyModel { GroupId = group.GroupId, Name = group.Name, SubGroups = new List<GroupHierarchyModel>() });
            }
            foreach (var tempGhm in ghm.SubGroups)
            {
                ifGroupBelongsToTree(ref belongs, tempGhm, mc, groupId);
            }
        }
        //-------------


        private void GetDeviceList(GroupHierarchyModel? ghm, ref List<Device> deviceList)
        {
            if (ghm == null)
                return;

            if (ghm.SubGroups != null && ghm.SubGroups.Count != 0)
            {
                foreach (var entry in ghm.SubGroups)
                {
                    GetDeviceList(entry, ref deviceList);
                }
            }
            else
            {
                if (ghm.Devices != null)
                    deviceList.AddRange(ghm.Devices);
            }
        }

        public static void CronJob()
        {
            DateTime now = DateTime.Now;
            DateTime dateTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);
            monitorContext mc = new monitorContext();
            List<Report> reports = mc.Reports.ToList();

            foreach (var rep in reports)
            {
                
                if (rep.NextDate.Equals(dateTime))
                {
                    if (rep.Frequency.Equals("Weekly", StringComparison.InvariantCultureIgnoreCase))
                    {
                        rep.NextDate = rep.NextDate.AddDays(7);

                    } else if (rep.Frequency.Equals("Monthly", StringComparison.InvariantCultureIgnoreCase))
                    {
                        rep.NextDate = rep.NextDate.AddMonths(1);

                    } else if (rep.Frequency.Equals("Daily", StringComparison.InvariantCultureIgnoreCase))
                    {
                        rep.NextDate = rep.NextDate.AddDays(1);

                    } else if (rep.Frequency.Equals("Yearly", StringComparison.InvariantCultureIgnoreCase))
                    {
                        rep.NextDate = rep.NextDate.AddYears(1);

                    }

                    mc.ReportInstances.Add(new ReportInstance() { Name = rep.Name + " " + rep.NextDate, ReportId = rep.ReportId, UriLink = "ftp://..." });
                    mc.SaveChanges();

                }

            }

        }

        public List<Device> GetDevicesForGHM(GroupHierarchyModel ghm)
        {
            List<Device> deviceList = new List<Device>();
            GetDeviceList(ghm, ref deviceList);

            return deviceList;
        }

        public static async Task<HttpResponseMessage> GetConfigFile(string JWT, Guid deviceUID, string fileName, string username)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JWT);


            var values = new Dictionary<string, string>
            {
                {"deviceUid", deviceUID.ToString()}, {"fileName", fileName}, {"path", ""}, {"user", username}
            };
            var content = JsonConvert.SerializeObject(values, Formatting.Indented);

            var data = new StringContent(content, Encoding.UTF8, "application/json");
            return await client.PostAsync("https://si-grupa5.herokuapp.com/api/web/agent/file/get", data);
        }

        public static async Task<HttpResponseMessage> PostConfigFile(string JWT, Guid deviceUID, string fileName, string username, string base64)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JWT);

            var values = new Dictionary<string, string>
            {
                {"deviceUid", deviceUID.ToString()}, {"fileName", fileName}, {"path", ""}, {"base64", base64}, {"user", username}
            };
            var content = JsonConvert.SerializeObject(values, Formatting.Indented);

            var data = new StringContent(content, Encoding.UTF8, "application/json");
            return await client.PostAsync("https://si-grupa5.herokuapp.com/api/web/agent/file/put", data);
        }


        public bool CheckBaseGroup(int? groupId1, int? groupId2)
        {
            monitorContext mc = new monitorContext();
            Group group1 = mc.Groups.Where(x => x.GroupId == groupId1).FirstOrDefault();
            Group group2 = mc.Groups.Where(x => x.GroupId == groupId2).FirstOrDefault();

            while(true)
            {
                Group parentGroup1 = mc.Groups.Where(x => x.GroupId == group1.ParentGroup).FirstOrDefault();
                if(parentGroup1.ParentGroup == null)
                {
                    break;
                }
                group1 = mc.Groups.Where(x => x.GroupId == group1.ParentGroup).FirstOrDefault();
            }

            while (true)
            {
                Group parentGroup2 = mc.Groups.Where(x => x.GroupId == group2.ParentGroup).FirstOrDefault();
                if (parentGroup2.ParentGroup == null)
                {
                    break;
                }
                group2 = mc.Groups.Where(x => x.GroupId == group2.ParentGroup).FirstOrDefault();
            }

            if (group1.Equals(group2))
            {
                return true;
            }

            return false;
        
        }


        public List<DeviceResponseModel> getDRMfromDeviceList(List<Device> devices, monitorContext mc)
        {
            List<DeviceResponseModel> drmList = new List<DeviceResponseModel>();
            foreach (var dev in devices)
            {
                int? groupIdForDevice = (from x in mc.DeviceGroups.OfType<DeviceGroup>() where x.DeviceId == dev.DeviceId select x.GroupId).FirstOrDefault();
                drmList.Add(new DeviceResponseModel()
                {
                    DeviceId = dev.DeviceId,
                    Name = dev.Name,
                    Location = dev.Location,
                    LocationLatitude = dev.LocationLatitude,
                    LocationLongitude = dev.LocationLongitude,
                    Status = dev.Status,
                    LastTimeOnline = dev.LastTimeOnline,
                    InstallationCode = dev.InstallationCode,
                    GroupId = groupIdForDevice,
                    GroupName = (from x in mc.Groups.OfType<Group>() where x.GroupId == groupIdForDevice select x.Name).FirstOrDefault(),
                    DeviceUid = dev.DeviceUid
                });
            }
            return drmList;
        }

    }
}
