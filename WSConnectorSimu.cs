using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LINQtoCSV;
using System.IO;

namespace WSConnectorSimu
{
    /* Describe la posicion de las columnas de la base de datos en el archivo CSV*/
    class Pcb
    {
        [CsvColumn(Name = "PartNumber", FieldIndex = 1)]
        public string PartNumber { get; set; }
        [CsvColumn(Name = "SerialNumber", FieldIndex = 2)]
        public string SerialNumber { get; set; }
        [CsvColumn(Name = "StationName", FieldIndex = 3)]
        public string StationName { get; set; }
        [CsvColumn(Name = "ProcessName", FieldIndex = 4)]
        public string ProcessName { get; set; }
        [CsvColumn(Name = "Pass_Fail", FieldIndex = 5)]
        public int Pass_Fail { get; set; }
        [CsvColumn(Name = "TimesTested", FieldIndex = 6)]
        public int TimesTested { get; set; }
        [CsvColumn(Name = "EntranceTime", FieldIndex = 7)]
        public string EntTime { get; set; }
        [CsvColumn(Name = "ExitTime", FieldIndex = 8)]
        public string ExtTime { get; set; }
        [CsvColumn(Name = "FailString", FieldIndex = 9)]
        public string FailString { get; set; }
        [CsvColumn(Name = "UserName", FieldIndex = 10)]
        public string UserName { get; set; }
    }
    /* Describe la posicion de las columnas del orden de Procesos archivo CSV*/
    class ProcessData
    {
        [CsvColumn(Name = "NumberProcess", FieldIndex = 1)]
        public int NumberProcess { get; set; }
        [CsvColumn(Name = "Station", FieldIndex = 2)]
        public string Station { get; set; }
        [CsvColumn(Name = "Process", FieldIndex = 3)]
        public string Process { get; set; }
    }
    /* Describe la posicion de las columnas de los seriales que el usuario registre*/
    class RegSerial
    {
        [CsvColumn(Name = "SerialReg", FieldIndex = 1)]
        public string SerialReg { get; set; }
        [CsvColumn(Name = "PartNumber", FieldIndex = 2)]
        public string PartNumber { get; set; }
    }

    public class Connector
    {
        string BasedataPath = @"C:\Traceability Simu\data\Basedata.csv"; //Basepath es la ruta estara la base de datos
        string ProcessPath = @"C:\Traceability Simu\data\Process.csv"; //Procesos es la base en donde se encuentra los procesos
        string SerialPath = @"C:\Traceability Simu\data\Serials.csv"; //Seriales registrados en la base de datos
        char SepChar = ';'; //Las columnas se separan por ;


        public string CIMP_PartNumberRef(string SerialNumber, int BCTYPE, ref string AssemblyPartNumber)
        {
            string str = "";
            try
            {
                CsvFileDescription inputFileDescriptionSP = new CsvFileDescription
                {
                    SeparatorChar = SepChar,
                    FirstLineHasColumnNames = true
                };
                CsvContext ccSP = new CsvContext();
                IEnumerable<RegSerial> serialRegs =
                    ccSP.Read<RegSerial>
                    (SerialPath, inputFileDescriptionSP);
                var SerialByName =
                    from p in serialRegs
                    where p.SerialReg == SerialNumber
                    select new { p.PartNumber };
                foreach (var item in SerialByName)
                {
                    AssemblyPartNumber = item.PartNumber;
                }
            }
            catch (Exception ex)
            {
                str = ex.Message;
            }
            return str;
        }

        public string BackCheck_Serial(string _serialNumber, string _stationName)
        {
            //BP = Lo obtiene de BaseDataPath
            //PP = Lo obtiene de ProcessPath
            //SP = Lo obtiene de SerialPath
            string SerialBP = "";
            string StationBP = "";
            string ProcessBP = "";
            int Pass_failBP = 0;
            string SerialSP = "";
            int NumberProcessPP = 0;
            string ProcessPP = "";
            string ProcessPP2 = "";
            string StationPP = "";
            string str = ""; //Respuesta de la funcion
            try
            {
                //Se obtiene el ultimo registro del serial capturado
                CsvFileDescription inputFileDescriptionBP = new CsvFileDescription
                {
                    SeparatorChar = SepChar,
                    FirstLineHasColumnNames = true
                };
                CsvContext ccBP = new CsvContext();
                IEnumerable<Pcb> pcbs =
                    ccBP.Read<Pcb>
                    (BasedataPath, inputFileDescriptionBP);
                var PcbByName =
                    from p in pcbs
                    where p.SerialNumber == _serialNumber
                    select new { p.SerialNumber, p.StationName, p.ProcessName, p.Pass_Fail };
                foreach (var item in PcbByName)
                {
                    SerialBP = item.SerialNumber;
                    StationBP = item.StationName;
                    ProcessBP = item.ProcessName;
                    Pass_failBP = item.Pass_Fail;
                }
                if (PcbByName.Count() == 0) //Si no encontro serial, consulta serial en SerialPath para saber si esta registrado
                {
                    CsvFileDescription inputFileDescriptionSP = new CsvFileDescription
                    {
                        SeparatorChar = SepChar,
                        FirstLineHasColumnNames = true
                    };
                    CsvContext ccSP = new CsvContext();
                    IEnumerable<RegSerial> serialRegs =
                        ccSP.Read<RegSerial>
                        (SerialPath, inputFileDescriptionSP);
                    var SerialByName =
                        from p in serialRegs
                        where p.SerialReg == _serialNumber
                        select new { p.SerialReg };
                    foreach (var item in SerialByName)
                    {
                        SerialSP = item.SerialReg;
                    }
                    if (_serialNumber == SerialSP && _stationName == "ITA_LASER06") //Se relaciona a la primera estacion
                    {
                        str = "1|OK: Unidad correcta SN: " + _serialNumber;
                    }
                    else if (_serialNumber == SerialSP && _stationName != "ITA_LASER06") //Se relaciona a la primera estacion
                    {
                        str = "0|Fail: No corresponde a proceso SN: " + _serialNumber;
                    }
                    else //Si no encuentra, el serial no esta registrado
                    {
                        str = "0|Fail: Serial no encontrado SN: " + _serialNumber;
                    }
                }
                // Para llegar aqui, el serial debe estar registrado en la base de datos local con la primera prueba realizada
                else if (Pass_failBP == 1) // Si la ultima prueba que capturo el serial es Pass, lee la base de relacion de proceso
                {
                    CsvFileDescription inputFileDescriptionPP = new CsvFileDescription //Consulta la base de datos de Procesos para obtener cual es el orden de la prueba
                    {
                        SeparatorChar = SepChar,
                        FirstLineHasColumnNames = true
                    };
                    CsvContext ccPP = new CsvContext();
                    IEnumerable<ProcessData> processDatas =
                        ccPP.Read<ProcessData>
                        (ProcessPath, inputFileDescriptionPP);
                    var ProcessByName =
                        from p in processDatas
                        where p.Station == StationBP
                        select new { p.NumberProcess, p.Process };
                    foreach (var item in ProcessByName)
                    {
                        NumberProcessPP = item.NumberProcess; //Obtiene el orden de la ultima prueba capturada del Serial
                        ProcessPP2 = item.Process; //Segunda variable en caso de ser la ultima estacion
                    }
                    // Se suma +1 para poder realizar una segunda consulta para obtener el proceso siguiente
                    NumberProcessPP = NumberProcessPP + 1;
                    //Se realiza nueva consulta para saber si la estacion capturada = estacion siguiente.
                    CsvFileDescription inputFileDescriptionPP2 = new CsvFileDescription
                    {
                        SeparatorChar = SepChar,
                        FirstLineHasColumnNames = true
                    };
                    CsvContext ccPP2 = new CsvContext();
                    IEnumerable<ProcessData> processDatas2 =
                        ccPP2.Read<ProcessData>
                        (ProcessPath, inputFileDescriptionPP2);
                    var ProcessByName2 =
                        from p in processDatas2
                        where p.NumberProcess == NumberProcessPP
                        select new { p.Station, p.Process };
                    foreach (var item in ProcessByName2)
                    {
                        StationPP = item.Station;
                        ProcessPP = item.Process;
                    }
                    if (StationPP == "" && ProcessPP == "") //Si ya es la ultima estacion de la base es shipping, devuelve 0|shipping, en P2 solo el fail
                    {
                        if (ProcessPP2.Substring(0, 4) != "SHIP")
                        {
                            str = "0|Fail: No corresponde a proceso SN: " + _serialNumber;
                        }
                        else
                        {
                            str = "0|Fail: No corresponde a proceso SN: " + _serialNumber;
                        }
                    }
                    else if (StationPP == _stationName) //Si la estacion siguiente a la ultima en el sistema es igual a la capturada
                    {
                        str = "1|OK: Unidad correcta SN: " + _serialNumber; //Backcheck correcto
                    }
                    else //Busca si el proceso capturado existe en la base de datos de procesos
                    {
                        CsvFileDescription inputFileDescriptionPP3 = new CsvFileDescription
                        {
                            SeparatorChar = SepChar,
                            FirstLineHasColumnNames = true
                        };
                        CsvContext ccPP3 = new CsvContext();
                        IEnumerable<ProcessData> processDatas3 =
                            ccPP3.Read<ProcessData>
                            (ProcessPath, inputFileDescriptionPP3);
                        var ProcessByName3 =
                            from p in processDatas3
                            where p.Station == _stationName
                            select new { p.Station };
                        foreach (var item in ProcessByName3)
                        {
                            StationPP = item.Station;
                        }
                        if (ProcessByName3.Count() > 0)
                        {
                            str = "0|Fail: No corresponde a proceso SN: " + _serialNumber; //Backcheck incorrecto, muestra estacion correcta
                        }
                        else
                        {
                            str = "0|Fail: Estacion no encontrada SN: " + _serialNumber;  // Backcheck incorrecto por estacion inexistente
                        }
                    }
                }
                else if (Pass_failBP == 0 && StationBP == _stationName) //Si la ultima prueba capturada en sistema es false, se permite realizar nuevamente
                {
                    str = "1|OK: Unidad correcta SN: " + _serialNumber;
                }
                else
                {
                    str = "0|Fail: No corresponde a proceso SN: " + _serialNumber;
                }
            }
            catch (Exception ex)
            {
                str = "0|Error: " + ex.Message;
            }
            return str;
        }

        public string InsertProcessDataWithFails(string ser_num, string station_name, string function, string ent_time, string ext_time, int pass_fail, string fail_string, string employee)
        {
            string str = "";
            int Pass_failBP = 0;
            int TimesTestedBP = 0;
            //Ruta donde esta el archivo temporal, bin\debug\tmp
            string tmpPath = "tmp.csv"; // . = working dir
            //throw new Exception("fail: " + tmp);
            try
            {
                //Consulta si el proceso que se va a insertar ya estaba registrado previamente
                CsvFileDescription inputFileDescriptionBP = new CsvFileDescription
                {
                    SeparatorChar = SepChar,
                    FirstLineHasColumnNames = true
                };
                CsvContext ccBP = new CsvContext();
                IEnumerable<Pcb> pcbs =
                    ccBP.Read<Pcb>
                    (BasedataPath, inputFileDescriptionBP);
                var PcbByName =
                    from p in pcbs
                    where p.SerialNumber == ser_num && p.StationName == station_name
                    select new { p.Pass_Fail, p.TimesTested };
                foreach (var item in PcbByName)
                {
                    Pass_failBP = item.Pass_Fail;
                    TimesTestedBP = item.TimesTested;
                }
                
                if (Pass_failBP == 0) // si la prueba no se paso, se suma 1 a los testeos hechos de esa prueba
                {
                    TimesTestedBP = TimesTestedBP + 1;
                    //Realiza el borrado de la linea al que se le hara de nuevo el proceso
                    StreamReader readerdelete;
                    StreamWriter writertmp;
                    string array;
                    bool finded;
                    finded = false;
                    string[] dato = new string[10];
                    char[] separator = { ';' };
                    readerdelete = File.OpenText(BasedataPath);
                    writertmp = File.CreateText(tmpPath);
                    array = readerdelete.ReadLine();
                    while (array != null && finded == false)
                    {
                        dato = array.Split(separator);
                        if (dato[1].Trim().Equals(ser_num) && dato[2].Trim().Equals(station_name))
                        {
                            //finded = true;
                        }
                        else
                        {
                            writertmp.WriteLine(array);
                        }
                        //Copia los registros a tmp.csv excepto los que sean iguales al serial y la estacion
                        array = readerdelete.ReadLine();
                    }
                    readerdelete.Close();
                    writertmp.Close();
                    //reemplaza el tmp por el archivo original
                    File.Delete(BasedataPath);
                    File.Move(tmpPath, BasedataPath);
                }
                else
                {
                    TimesTestedBP = 1;
                };
                //Se realiza consulta para obtener NumPart
                string partnumber = "";
                CIMP_PartNumberRef(ser_num, 1, ref partnumber);
                //Se abre archivo y se escribe insert process
                StreamWriter writer = new StreamWriter(BasedataPath, true); // Objeto que escribe
                string contenido = null;
                contenido = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9}", partnumber, ser_num, station_name, function, pass_fail, TimesTestedBP, ent_time, ext_time, fail_string, employee);
                writer.WriteLine(contenido);
                // Cierre de flujo
                writer.Close();
                str = "OK | Insertado Correctamente";
            }
            catch (Exception writefail)
            {
                str = writefail.Message;
            }
            return str;
        }

        public void CIMP_GetDateTimeStr(out string Date)
        {
            Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}