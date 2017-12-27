using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using NUnit.Framework;

namespace OpenMcdf.Test
{
    /// <summary>
    /// Summary description for CompoundFileTest
    /// </summary>
    [TestFixture]
    public class CompoundFileTest
    {
        [Test]
        public void Test_COMPRESS_SPACE()
        {
            String FILENAME = "MultipleStorage3.cfs"; // 22Kb

            FileInfo srcFile = new FileInfo(FILENAME);

            File.Copy(FILENAME, "MultipleStorage_Deleted_Compress.cfs", true);

            CompoundFile cf = new CompoundFile("MultipleStorage_Deleted_Compress.cfs", CFSUpdateMode.Update,
                CFSConfiguration.SectorRecycle | CFSConfiguration.EraseFreeSectors);

            CFStorage st = cf.RootStorage.GetStorage("MyStorage");
            st = st.GetStorage("AnotherStorage");

            Assert.IsNotNull(st);
            st.Delete("Another2Stream");
            cf.Commit();
            cf.Close();

            CompoundFile.ShrinkCompoundFile("MultipleStorage_Deleted_Compress.cfs"); // -> 7Kb

            FileInfo dstFile = new FileInfo("MultipleStorage_Deleted_Compress.cfs");

            Assert.IsTrue(srcFile.Length > dstFile.Length);
        }

        [Test]
        public void Test_ENTRY_NAME_LENGTH()
        {
            //Thanks to Mark Bosold for bug fix and unit

            CompoundFile cf = new CompoundFile();

            // Cannot be equal.
            string maxCharactersStreamName = "1234567890123456789A12345678901"; // 31 chars
            string maxCharactersStorageName = "1234567890123456789012345678901"; // 31 chars

            // Try Storage entry name with max characters.
            Assert.IsNotNull(cf.RootStorage.AddStorage(maxCharactersStorageName));
            CFStorage strg = cf.RootStorage.GetStorage(maxCharactersStorageName);
            Assert.IsNotNull(strg);
            Assert.IsTrue(strg.Name == maxCharactersStorageName);


            // Try Stream entry name with max characters.
            Assert.IsNotNull(cf.RootStorage.AddStream(maxCharactersStreamName));
            CFStream strm = cf.RootStorage.GetStream(maxCharactersStreamName);
            Assert.IsNotNull(strm);
            Assert.IsTrue(strm.Name == maxCharactersStreamName);

            string tooManyCharactersEntryName = "12345678901234567890123456789012"; // 32 chars

            try
            {
                // Try Storage entry name with too many characters.
                cf.RootStorage.AddStorage(tooManyCharactersEntryName);
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is CFException);
            }

            try
            {
                // Try Stream entry name with too many characters.
                cf.RootStorage.AddStream(tooManyCharactersEntryName);
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is CFException);
            }

            cf.Save("EntryNameLength");
            cf.Close();
        }

        [Test]
        public void Test_DELETE_WITHOUT_COMPRESSION()
        {
            String FILENAME = "MultipleStorage3.cfs";

            FileInfo srcFile = new FileInfo(FILENAME);

            CompoundFile cf = new CompoundFile(FILENAME);

            CFStorage st = cf.RootStorage.GetStorage("MyStorage");
            st = st.GetStorage("AnotherStorage");

            Assert.IsNotNull(st);

            st.Delete("Another2Stream"); //17Kb

            //cf.CompressFreeSpace();
            cf.Save("MultipleStorage_Deleted_Compress.cfs");

            cf.Close();
            FileInfo dstFile = new FileInfo("MultipleStorage_Deleted_Compress.cfs");

            Assert.IsFalse(srcFile.Length > dstFile.Length);
        }

        [Test]
        public void Test_WRITE_AND_READ_CFS_VERSION_4()
        {
            String filename = "WRITE_AND_READ_CFS_V4.cfs";

            CompoundFile cf = new CompoundFile(CFSVersion.Ver_4,
                CFSConfiguration.EraseFreeSectors | CFSConfiguration.SectorRecycle);

            CFStorage st = cf.RootStorage.AddStorage("MyStorage");
            CFStream sm = st.AddStream("MyStream");
            byte[] b = new byte[220];
            sm.SetData(b);

            cf.Save(filename);
            cf.Close();

            CompoundFile cf2 = new CompoundFile(filename);
            CFStorage st2 = cf2.RootStorage.GetStorage("MyStorage");
            CFStream sm2 = st2.GetStream("MyStream");

            Assert.IsNotNull(sm2);
            Assert.IsTrue(sm2.Size == 220);

            cf2.Close();
        }

        [Test]
        public void Test_WRITE_READ_CFS_VERSION_4_STREAM()
        {
            String filename = "WRITE_COMMIT_READ_CFS_V4.cfs";

            CompoundFile cf = new CompoundFile(CFSVersion.Ver_4,
                CFSConfiguration.SectorRecycle | CFSConfiguration.EraseFreeSectors);

            CFStorage st = cf.RootStorage.AddStorage("MyStorage");
            CFStream sm = st.AddStream("MyStream");
            byte[] b = Helpers.GetBuffer(227);
            sm.SetData(b);

            cf.Save(filename);
            cf.Close();

            CompoundFile cf2 = new CompoundFile(filename);
            CFStorage st2 = cf2.RootStorage.GetStorage("MyStorage");
            CFStream sm2 = st2.GetStream("MyStream");

            Assert.IsNotNull(sm2);
            Assert.IsTrue(sm2.Size == b.Length);

            cf2.Close();
        }

        [Test]
        public void Test_OPEN_FROM_STREAM()
        {
            String filename = "reportREAD.xls";
            File.Copy(filename, "reportOPENFROMSTREAM.xls", true);
            FileStream fs = new FileStream(filename, FileMode.Open);
            CompoundFile cf = new CompoundFile(fs);
            CFStream foundStream = cf.RootStorage.GetStream("Workbook");

            byte[] temp = foundStream.GetData();

            Assert.IsNotNull(temp);

            cf.Close();
        }

        [Test]
        public void Test_MULTIPLE_SAVE()
        {
            var file = new CompoundFile();

            file.Save("test.mdf");

            var meta = file.RootStorage.AddStream("meta");

            meta.Append(BitConverter.GetBytes(DateTime.Now.ToBinary()));
            meta.Append(BitConverter.GetBytes(DateTime.Now.ToBinary()));

            file.Save("test.mdf");
        }

        [Test]
        public void Test_OPEN_COMPOUND_BUG_FIX_133()
        {
            var f = new CompoundFile("testbad.ole");
            CFStream cfs = f.RootStorage.GetStream("\x01Ole10Native");
            byte[] data = cfs.GetData();
            Assert.IsTrue(data.Length == 18140);
        }

        [Test]
        public void Test_COMPARE_DIR_ENTRY_NAME_BUG_FIX_ID_3487353()
        {
            var f = new CompoundFile("report_name_fix.xls", CFSUpdateMode.Update,
                CFSConfiguration.SectorRecycle | CFSConfiguration.EraseFreeSectors);
            CFStream cfs = f.RootStorage.AddStream("Poorbook");
            cfs.Append(Helpers.GetBuffer(20));
            f.Commit();
            f.Close();

            f = new CompoundFile("report_name_fix.xls", CFSUpdateMode.Update,
                CFSConfiguration.SectorRecycle | CFSConfiguration.EraseFreeSectors);
            cfs = f.RootStorage.GetStream("Workbook");
            Assert.IsTrue(cfs.Name == "Workbook");
            f.RootStorage.Delete("PoorBook");
            f.Commit();
            f.Close();
        }

        [Test]
        public void Test_GET_COMPOUND_VERSION()
        {
            var f = new CompoundFile("report_name_fix.xls");
            CFSVersion ver = f.Version;

            Assert.IsTrue(ver == CFSVersion.Ver_3);

            f.Close();
        }

        [Test]
        public void Test_FUNCTIONAL_BEHAVIOUR()
        {
            //System.Diagnostics.Trace.Listeners.Add(new ConsoleTraceListener());

            const int N_FACTOR = 1;

            byte[] bA = Helpers.GetBuffer(20 * 1024 * N_FACTOR, 0x0A);
            byte[] bB = Helpers.GetBuffer(5 * 1024, 0x0B);
            byte[] bC = Helpers.GetBuffer(5 * 1024, 0x0C);
            byte[] bD = Helpers.GetBuffer(5 * 1024, 0x0D);
            byte[] bE = Helpers.GetBuffer(8 * 1024 * N_FACTOR + 1, 0x1A);
            byte[] bF = Helpers.GetBuffer(16 * 1024 * N_FACTOR, 0x1B);
            byte[] bG = Helpers.GetBuffer(14 * 1024 * N_FACTOR, 0x1C);
            byte[] bH = Helpers.GetBuffer(12 * 1024 * N_FACTOR, 0x1D);
            byte[] bE2 = Helpers.GetBuffer(8 * 1024 * N_FACTOR, 0x2A);
            byte[] bMini = Helpers.GetBuffer(1027, 0xEE);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            //############

            // Phase 1
            using (var cf = new CompoundFile(CFSVersion.Ver_3, CFSConfiguration.SectorRecycle))
            {
                cf.RootStorage.AddStream("A").SetData(bA);
                cf.Save("OneStream.cfs");
                cf.Close();
            }

            // Test Phase 1
            using (var cfTest = new CompoundFile("OneStream.cfs"))
            {
                CFStream testSt = cfTest.RootStorage.GetStream("A");

                Assert.IsNotNull(testSt);
                Assert.IsTrue(testSt.Size == bA.Length);
                Assert.IsTrue(Helpers.CompareBuffer(bA, testSt.GetData()));

                cfTest.Close();
            }

            //###########

            //Phase 2
            using (var cf = new CompoundFile("OneStream.cfs", CFSUpdateMode.ReadOnly, CFSConfiguration.SectorRecycle))
            {
                cf.RootStorage.AddStream("B").SetData(bB);
                cf.RootStorage.AddStream("C").SetData(bC);
                cf.RootStorage.AddStream("D").SetData(bD);
                cf.RootStorage.AddStream("E").SetData(bE);
                cf.RootStorage.AddStream("F").SetData(bF);
                cf.RootStorage.AddStream("G").SetData(bG);
                cf.RootStorage.AddStream("H").SetData(bH);

                cf.Save("8_Streams.cfs");
                cf.Close();
            }

            // Test Phase 2


            using (var cfTest = new CompoundFile("8_Streams.cfs"))
            {
                CFStream testSt = cfTest.RootStorage.GetStream("B");
                Assert.IsNotNull(testSt);
                Assert.IsTrue(testSt.Size == bB.Length);
                Assert.IsTrue(Helpers.CompareBuffer(bB, testSt.GetData()));

                testSt = cfTest.RootStorage.GetStream("C");
                Assert.IsNotNull(testSt);
                Assert.IsTrue(testSt.Size == bC.Length);
                Assert.IsTrue(Helpers.CompareBuffer(bC, testSt.GetData()));

                testSt = cfTest.RootStorage.GetStream("D");
                Assert.IsNotNull(testSt);
                Assert.IsTrue(testSt.Size == bD.Length);
                Assert.IsTrue(Helpers.CompareBuffer(bD, testSt.GetData()));

                testSt = cfTest.RootStorage.GetStream("E");
                Assert.IsNotNull(testSt);
                Assert.IsTrue(testSt.Size == bE.Length);
                Assert.IsTrue(Helpers.CompareBuffer(bE, testSt.GetData()));

                testSt = cfTest.RootStorage.GetStream("F");
                Assert.IsNotNull(testSt);
                Assert.IsTrue(testSt.Size == bF.Length);
                Assert.IsTrue(Helpers.CompareBuffer(bF, testSt.GetData()));

                testSt = cfTest.RootStorage.GetStream("G");
                Assert.IsNotNull(testSt);
                Assert.IsTrue(testSt.Size == bG.Length);
                Assert.IsTrue(Helpers.CompareBuffer(bG, testSt.GetData()));

                testSt = cfTest.RootStorage.GetStream("H");
                Assert.IsNotNull(testSt);
                Assert.IsTrue(testSt.Size == bH.Length);
                Assert.IsTrue(Helpers.CompareBuffer(bH, testSt.GetData()));

                cfTest.Close();
            }

            File.Copy("8_Streams.cfs", "6_Streams.cfs", true);
            File.Delete("8_Streams.cfs");

            //###########

            Trace.Listeners.Add(new ConsoleTraceListener());

            // Phase 3
            using (var cf = new CompoundFile("6_Streams.cfs", CFSUpdateMode.Update,
                CFSConfiguration.SectorRecycle | CFSConfiguration.EraseFreeSectors))
            {
                cf.RootStorage.Delete("D");
                cf.RootStorage.Delete("G");
                cf.Commit();

                cf.Close();
            }

            //Test Phase 3
            using (var cfTest = new CompoundFile("6_Streams.cfs", CFSUpdateMode.ReadOnly, CFSConfiguration.Default))
            {
                bool catched = false;

                try
                {
                    var testSt = cfTest.RootStorage.GetStream("D");
                }
                catch (Exception ex)
                {
                    if (ex is CFItemNotFound)
                        catched = true;
                }

                Assert.IsTrue(catched);

                catched = false;

                try
                {
                    var testSt = cfTest.RootStorage.GetStream("G");
                }
                catch (Exception ex)
                {
                    if (ex is CFItemNotFound)
                        catched = true;
                }

                Assert.IsTrue(catched);

                cfTest.Close(true);
            }

            //##########

            // Phase 4

            File.Copy("6_Streams.cfs", "6_Streams_Shrinked.cfs", true);
            CompoundFile.ShrinkCompoundFile("6_Streams_Shrinked.cfs");

            // Test Phase 4

            Assert.IsTrue(new FileInfo("6_Streams_Shrinked.cfs").Length < new FileInfo("6_Streams.cfs").Length);

            using (var cfTest = new CompoundFile("6_Streams_Shrinked.cfs"))
            {
                Action<CFItem> va = delegate(CFItem item)
                {
                    if (item.IsStream)
                    {
                        CFStream ia = item as CFStream;
                        Assert.IsNotNull(ia);
                        Assert.IsTrue(ia.Size > 0);
                        byte[] d = ia.GetData();
                        Assert.IsNotNull(d);
                        Assert.IsTrue(d.Length > 0);
                        Assert.IsTrue(d.Length == ia.Size);
                    }
                };

                cfTest.RootStorage.VisitEntries(va, true);
                cfTest.Close();
            }

            //##########

            //Phase 5

            using (var cf = new CompoundFile("6_Streams_Shrinked.cfs", CFSUpdateMode.Update,
                CFSConfiguration.SectorRecycle))
            {
                cf.RootStorage.AddStream("ZZZ").SetData(bF);
                cf.RootStorage.GetStream("E").Append(bE2);
                cf.Commit();
                cf.Close();
            }


            using (var cf = new CompoundFile("6_Streams_Shrinked.cfs", CFSUpdateMode.Update,
                CFSConfiguration.SectorRecycle))
            {
                cf.RootStorage.CLSID = new Guid("EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE");
                cf.Commit();
                cf.Close();
            }

            using (var cf = new CompoundFile("6_Streams_Shrinked.cfs", CFSUpdateMode.Update,
                CFSConfiguration.SectorRecycle))
            {
                cf.RootStorage.AddStorage("MyStorage").AddStream("ZIP").Append(bE);
                cf.Commit();
                cf.Close();
            }

            using (var cf = new CompoundFile("6_Streams_Shrinked.cfs", CFSUpdateMode.Update,
                CFSConfiguration.SectorRecycle))
            {
                cf.RootStorage.AddStorage("AnotherStorage").AddStream("ANS").Append(bE);
                cf.RootStorage.Delete("MyStorage");


                cf.Commit();
                cf.Close();
            }

            //Test Phase 5

            //#####

            //Phase 6

            using (var cf = new CompoundFile("6_Streams_Shrinked.cfs", CFSUpdateMode.Update,
                CFSConfiguration.SectorRecycle))
            {
                CFStorage root = cf.RootStorage;

                root.AddStorage("MiniStorage").AddStream("miniSt").Append(bMini);

                cf.RootStorage.GetStorage("MiniStorage").AddStream("miniSt2").Append(bMini);
                cf.Commit();
                cf.Close();
            }

            using (var cf = new CompoundFile("6_Streams_Shrinked.cfs", CFSUpdateMode.Update,
                CFSConfiguration.SectorRecycle))
            {
                cf.RootStorage.GetStorage("MiniStorage").Delete("miniSt");


                cf.RootStorage.GetStorage("MiniStorage").GetStream("miniSt2").Append(bE);

                cf.Commit();
                cf.Close();
            }

            //Test Phase 6

            using (var cfTest = new CompoundFile("6_Streams_Shrinked.cfs"))
            {
                byte[] d2 = cfTest.RootStorage.GetStorage("MiniStorage").GetStream("miniSt2")
                    .GetData();
                Assert.IsTrue(d2.Length == (bE.Length + bMini.Length));

                int cnt = 1;
                byte[] buf = new byte[cnt];
                cnt = cfTest.RootStorage.GetStorage("MiniStorage").GetStream("miniSt2")
                    .Read(buf, bMini.Length, cnt);

                Assert.IsTrue(cnt == 1);
                Assert.IsTrue(buf[0] == 0x1A);

                cnt = 1;
                cnt = cfTest.RootStorage.GetStorage("MiniStorage").GetStream("miniSt2")
                    .Read(buf, bMini.Length - 1, cnt);
                Assert.IsTrue(cnt == 1);
                Assert.IsTrue(buf[0] == 0xEE);

                try
                {
                    cfTest.RootStorage.GetStorage("MiniStorage").GetStream("miniSt");
                }
                catch (Exception ex)
                {
                    Assert.IsTrue(ex is CFItemNotFound);
                }

                cfTest.Close();
            }

            //##############

            //Phase 7

            using (var cf = new CompoundFile("6_Streams_Shrinked.cfs", CFSUpdateMode.Update,
                CFSConfiguration.SectorRecycle))
            {

                cf.RootStorage.GetStorage("MiniStorage").GetStream("miniSt2").SetData(bA);
                cf.Commit();
                cf.Close();
            }


            //Test Phase 7

            using (var cf = new CompoundFile("6_Streams_Shrinked.cfs", CFSUpdateMode.Update,
                CFSConfiguration.SectorRecycle))
            {
                var d2 = cf.RootStorage.GetStorage("MiniStorage").GetStream("miniSt2")
                    .GetData();
                Assert.IsNotNull(d2);
                Assert.IsTrue(d2.Length == bA.Length);

                cf.Close();
            }

            //##############

            using (var cf = new CompoundFile("6_Streams_Shrinked.cfs",
                CFSUpdateMode.ReadOnly, CFSConfiguration.SectorRecycle))
            {

                var myStream = cf.RootStorage.GetStream("C");
                var data = myStream.GetData();
                Console.WriteLine(data[0] + " : " + data[data.Length - 1]);

                myStream = cf.RootStorage.GetStream("B");
                data = myStream.GetData();
                Console.WriteLine(data[0] + " : " + data[data.Length - 1]);

                cf.Close();
            }

            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);
        }

        [Test]
        public void Test_RETRIVE_ALL_NAMED_ENTRIES()
        {
            var f = new CompoundFile("MultipleStorage4.cfs");
            IList<CFItem> result = f.GetAllNamedEntries("MyStream");

            Assert.IsTrue(result.Count == 3);
        }


        [Test]
        public void Test_CORRUPTED_CYCLIC_FAT_CHECK()
        {
            CompoundFile f = null;
            try
            {
                f = new CompoundFile("CyclicFAT.cfs");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is CFCorruptedFileException);
            }
            finally
            {
                if (f != null)
                    f.Close();
            }
        }

        [Test]
        public void Test_DIFAT_CHECK()
        {
            CompoundFile f = null;
            try
            {
                f = new CompoundFile();
                CFStream st = f.RootStorage.AddStream("LargeStream");
                st.Append(Helpers.GetBuffer(20000000, 0x0A)); //Forcing creation of two DIFAT sectors
                byte[] b1 = Helpers.GetBuffer(3, 0x0B);
                st.Append(b1); //Forcing creation of two DIFAT sectors

                f.Save("$OpenMcdf$LargeFile.cfs");

                f.Close();

                int cnt = 3;
                f = new CompoundFile("$OpenMcdf$LargeFile.cfs");

                byte[] b2 = new byte[cnt];
                cnt = f.RootStorage.GetStream("LargeStream").Read(b2, 20000000, cnt);
                f.Close();
                Assert.IsTrue(Helpers.CompareBuffer(b1, b2));
            }
            finally
            {
                if (f != null)
                    f.Close();

                if (File.Exists("$OpenMcdf$LargeFile.cfs"))
                    File.Delete("$OpenMcdf$LargeFile.cfs");
            }
        }

        [Test]
        public void Test_ADD_LARGE_NUMBER_OF_ITEMS()
        {
            int ITEM_NUMBER = 10000;

            CompoundFile f = null;
            byte[] buffer = Helpers.GetBuffer(10, 0x0A);
            try
            {
                f = new CompoundFile();

                for (int i = 0; i < ITEM_NUMBER; i++)
                {
                    CFStream st = f.RootStorage.AddStream("Stream" + i.ToString());
                    st.Append(buffer);
                }


                if (File.Exists("$ItemsLargeNumber.cfs"))
                    File.Delete("$ItemsLargeNumber.cfs");

                f.Save("$ItemsLargeNumber.cfs");
                f.Close();

                f = new CompoundFile("$ItemsLargeNumber.cfs");
                CFStream cfs = f.RootStorage.GetStream("Stream" + (ITEM_NUMBER / 2).ToString());

                Assert.IsTrue(cfs != null, "Item is null");
                Assert.IsTrue(Helpers.CompareBuffer(cfs.GetData(), buffer), "Items are different");
                f.Close();
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
            finally
            {
                //if (File.Exists("$ItemsLargeNumber.cfs"))
                //    File.Delete("$ItemsLargeNumber.cfs");
            }
        }

        [Test]
        public void Test_FIX_BUG_16_CORRUPTED_AFTER_RESIZE()
        {
            const string FILE_PATH = @"BUG_16_.xls";

            CompoundFile cf = new CompoundFile(FILE_PATH);

            CFStream dirStream = cf.RootStorage.GetStorage("_VBA_PROJECT_CUR").GetStorage("VBA").GetStream("dir");

            byte[] currentData = dirStream.GetData();

            Array.Resize(ref currentData, currentData.Length - 50);

            dirStream.SetData(currentData);

            cf.Save(FILE_PATH + ".edited");
            cf.Close();
        }


        [Test]
        public void Test_FIX_BUG_17_CORRUPTED_PPT_FILE()
        {
            const string FILE_PATH = @"2_MB-W.ppt";

            using (CompoundFile file = new CompoundFile(FILE_PATH))
            {
                //CFStorage dataSpaceInfo = file.RootStorage.GetStorage("\u0006DataSpaces").GetStorage("DataSpaceInfo");
                CFItem dsiItem = file.GetAllNamedEntries("DataSpaceInfo").FirstOrDefault();
            }
        }

        [Test]
        public void Test_FIX_BUG_24_CORRUPTED_THUMBS_DB_FILE()
        {
            try
            {
                using (var cf = new CompoundFile("_thumbs_bug_24.db"))
                {
                    cf.RootStorage.VisitEntries(item => Console.WriteLine(item.Name), recursive: false);
                }
            }
            catch (Exception exc)
            {
                Assert.IsInstanceOf<CFCorruptedFileException>(exc);
            }
        }

        [Test]
        public void Test_FIX_BUG_28_CompoundFile_Delete_ChildElementMaintainsFiles()
        {
            using (var compoundFile = new CompoundFile())
            {
                var storage1 = compoundFile.RootStorage.AddStorage("A");
                var storage2 = compoundFile.RootStorage.AddStorage("B");
                var storage3 = compoundFile.RootStorage.AddStorage("C");
                storage1.AddStream("A.1");
                compoundFile.RootStorage.Delete("B");
                storage1 = compoundFile.RootStorage.GetStorage("A");
                storage1.GetStream("A.1");
            }
        }

        [Test]
        public void Test_CORRUPTEDDOC_BUG36_SHOULD_THROW_CORRUPTED_FILE_EXCEPTION()
        {
            FileStream fs = null;
            try
            {
                fs = new FileStream("CorruptedDoc_bug36.doc", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                CompoundFile file = new CompoundFile(fs, CFSUpdateMode.ReadOnly, CFSConfiguration.LeaveOpen);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(fs.CanRead && fs.CanSeek && fs.CanWrite);
            }
        }

        [Test]
        public void Test_ISSUE_2_WRONG_CUTOFF_SIZE()
        {
            FileStream fs = null;
            try
            {
                if (File.Exists("TEST_ISSUE_2"))
                {
                    File.Delete("TEST_ISSUE_2");
                }

                CompoundFile cf = new CompoundFile(CFSVersion.Ver_3, CFSConfiguration.Default);
                var s = cf.RootStorage.AddStream("miniToNormal");
                s.Append(Helpers.GetBuffer(4090, 0xAA));

                cf.Save("TEST_ISSUE_2");
                cf.Close();
                var cf2 = new CompoundFile("TEST_ISSUE_2", CFSUpdateMode.Update, CFSConfiguration.Default);
                cf2.RootStorage.GetStream("miniToNormal").Append(Helpers.GetBuffer(6, 0xBB));
                cf2.Commit();
                cf2.Close();
            }
            catch (Exception ex)
            {
                Assert.IsTrue(fs.CanRead && fs.CanSeek && fs.CanWrite);
            }
        }

        [Test]
        public void Test_PR_13()
        {
            CompoundFile cf = new CompoundFile("report.xls");
            Guid g = cf.getGuidBySID(0);
            Assert.IsNotNull(g);
            g = cf.getGuidForStream(3);
            Assert.IsNotNull(g);
            Assert.IsTrue(!String.IsNullOrEmpty(cf.GetNameDirEntry(2)));
            Assert.IsTrue(cf.GetNumDirectories() > 0);
        }
        //[Test]
        //public void Test_CORRUPTED_CYCLIC_DIFAT_VALIDATION_CHECK()
        //{

        //    CompoundFile cf = null;
        //    try
        //    {
        //        cf = new CompoundFile("CiclycDFAT.cfs");
        //        CFStorage s = cf.RootStorage.GetStorage("MyStorage");
        //        CFStream st = s.GetStream("MyStream");
        //        Assert.IsTrue(st.Size > 0);
        //    }
        //    catch (Exception ex)
        //    {
        //        Assert.IsTrue(ex is CFCorruptedFileException);
        //    }
        //    finally
        //    {
        //        if (cf != null)
        //        {
        //            cf.Close();
        //        }
        //    }
        //}
        //[Test]
        //public void Test_REM()
        //{
        //    var f = new CompoundFile();

        //    byte[] bB = Helpers.GetBuffer(5 * 1024, 0x0B); 
        //    f.RootStorage.AddStream("Test").AppendData(bB);
        //    f.Save("Astorage.cfs");
        //}

        public void Test_COPY_ENTRIES_FROM_TO_STORAGE()
        {
            CompoundFile cfDst = new CompoundFile();
            CompoundFile cfSrc = new CompoundFile("MultipleStorage4.cfs");

            Copy(cfSrc.RootStorage, cfDst.RootStorage);

            cfDst.Save("MultipleStorage4Copy.cfs");

            cfDst.Close();
            cfSrc.Close();
        }

        #region Copy heper method

        /// <summary>
        /// Copies the given <paramref name="source"/> to the given <paramref name="destination"/>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        public static void Copy(CFStorage source, CFStorage destination)
        {
            source.VisitEntries(action =>
            {
                if (action.IsStorage)
                {
                    var destionationStorage = destination.AddStorage(action.Name);
                    destionationStorage.CLSID = action.CLSID;
                    destionationStorage.CreationDate = action.CreationDate;
                    destionationStorage.ModifyDate = action.ModifyDate;
                    Copy(action as CFStorage, destionationStorage);
                }
                else
                {
                    var sourceStream = action as CFStream;
                    var destinationStream = destination.AddStream(action.Name);
                    if (sourceStream != null) destinationStream.SetData(sourceStream.GetData());
                }
            }, false);
        }

        #endregion

        //[Test]
        //public void Test_CORRUPTED_CYCLIC_DIFAT_VALIDATION_CHECK()
        //{

        //    CompoundFile cf = null;
        //    try
        //    {
        //        cf = new CompoundFile("CiclycDFAT.cfs");
        //        CFStorage s = cf.RootStorage.GetStorage("MyStorage");
        //        CFStream st = s.GetStream("MyStream");
        //        Assert.IsTrue(st.Size > 0);
        //    }
        //    catch (Exception ex)
        //    {
        //        Assert.IsTrue(ex is CFCorruptedFileException);
        //    }
        //    finally
        //    {
        //        if (cf != null)
        //        {
        //            cf.Close();
        //        }
        //    }
        //}
        //[Test]
        //public void Test_REM()
        //{
        //    var f = new CompoundFile();

        //    byte[] bB = Helpers.GetBuffer(5 * 1024, 0x0B); 
        //    f.RootStorage.AddStream("Test").AppendData(bB);
        //    f.Save("Astorage.cfs");
        //}
    }
}