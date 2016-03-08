using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using Microsoft.VisualBasic.FileIO;

namespace Quote
{
    class Program
    {
        // class variables
        static uint MAX_LOAN_OFFER = 1000000;
        static uint MAX_LOAN_REQUEST = 15000;
        static uint MIN_LOAN_REQUEST = 1000;
        static uint LOAN_REQUEST_INC = 100;
        static double MAX_OFFER_RATE = 1.0;
        static double MIN_OFFER_RATE = 0.01;
        static uint LOAN_DURATION = 36;
        struct t_offer {public double rate;
                        public double monthly_repayment;
                        public double total_repayment;   
                        }

        static t_offer offer = new t_offer();
        static uint LoanRequested;

        static String[] COLUMN_NAMES = { "Lender", "Rate", "Available" };
        static Type[] COLUMN_TYPES = {typeof(string), typeof(double), typeof(uint)};

        delegate bool ParserDel(string str, out object value);

        static Dictionary<String,Delegate> rowParsers = _CreateParsers();
        static DataTable lendingOffersTable = new DataTable();
        
  
        // Main program
        static void Main(string[] args)
        {
            if (_checkArgsIn(args) != true)
            {
                _printUsageMsg();
                return;
            }

            if (_GetLoanOffer(args[1], out offer))
            {
                _printLoanOffer();
            }
            else {
                Console.WriteLine("It is not possible to provide quote at this time.");
            }
        }


        private static void _printLoanOffer()
        {
            string[] msg = {"RequestedAmmount: £" + LoanRequested.ToString("F0"),
                            "Rate: " + (100 * offer.rate).ToString("F1") + "%",
                            "Monthly repayment: £" + offer.monthly_repayment.ToString("F2"),
                            "Total repayment: £" + offer.total_repayment.ToString("F2")};
            _ConsoleMsg(msg);
        }

        private static bool _GetLoanOffer(string p, out t_offer offer)
        {
            t_offer temp_offer = new t_offer();
            bool status;
            uint totalLoanOfferSum = Convert.ToUInt32(lendingOffersTable.Compute("SUM(Available)", string.Empty));

            if (totalLoanOfferSum < LoanRequested) {
                offer = temp_offer;
                return false;
            }
            
            

            // sort table
            lendingOffersTable.DefaultView.Sort = "Rate ASC, Available DESC";
            lendingOffersTable = lendingOffersTable.DefaultView.ToTable();
            
            
            double rate = 0;
            // getting best rate
            status = _calculateBestRate(out rate);

            if (status)
            {
                temp_offer.rate = rate;

                temp_offer.monthly_repayment = _calculateRepayment(rate);
                temp_offer.total_repayment = LOAN_DURATION * temp_offer.monthly_repayment;

                offer = temp_offer;
                return true;
            }
            else {
                offer = temp_offer;
                return false;
            }
                           

        }

        private static double _calculateRepayment(double rate)
        {
            double loanMi = (rate / 12);
            uint numMonths = LOAN_DURATION;
            double negNumMonths = 0 - Convert.ToDouble(numMonths);
            double monthlyPayment = LoanRequested * loanMi / (1 - System.Math.Pow((1 + loanMi), negNumMonths));
            double totalPayment = monthlyPayment * numMonths;
            return (monthlyPayment);
        }

        private static bool _calculateBestRate(out double rate)
        {
            uint temp_rem = LoanRequested;
            double temp_rate = 0;

            // filling the loan request
            foreach (DataRow row in lendingOffersTable.Rows)
            {
                double curr_rate = Convert.ToDouble(row["Rate"]);
                uint curr_avail = Convert.ToUInt32(row["Available"]);

                if (temp_rem > 0 && temp_rem <= curr_avail)
                {
                    temp_rate += curr_rate * temp_rem;
                    temp_rem = 0;
                }
                else if (temp_rem > 0)
                {
                    temp_rate += curr_rate * curr_avail;
                    temp_rem -= curr_avail;
                }

                if (temp_rem == 0)
                {
                    // weigthed rate is divided by total ammount
                    temp_rate /= LoanRequested;

                    break;
                }

            }

            if (temp_rem == 0)
            {
                rate = temp_rate;
                return true;
            }
            else {
                rate = 0;
                return false;
            }
        }

        // parsers for fields
        private static Dictionary<String,Delegate> _CreateParsers()
        {
 	            var parsers = new Dictionary<String, Delegate>();
                _AddParser(parsers, "Lender", _parseLender);
                _AddParser(parsers, "Available", _parseAvailable);
                _AddParser(parsers, "Rate", _parseRate);    
                return parsers;
        }


        private static void _AddParser(Dictionary<String, Delegate> parsers, String column, ParserDel parser)
        {
            parsers[column] = parser;
        }

        private static bool _parseAvailable(String input, out object value )
        {
 	        uint temp;
            if (uint.TryParse(input, out temp)) {
                
                if (temp > 0 && temp < MAX_LOAN_OFFER) {
                    value = temp;
                    return true;
                }
                else {
                    value = 0;
                    return false;
                }
            }
            else {
                value = 0;
                return false;
            }

        }

        private static bool _parseLender(String input, out object value)
        {
 	        value = input.Trim();
            string sPattern = "^[A-Z][a-z]*$";

            if (System.Text.RegularExpressions.Regex.IsMatch(input, sPattern)) {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool _parseRate(String input, out object value)
        {
            double temp;
            if (double.TryParse(input, out temp)) {

                if (temp >= MIN_OFFER_RATE && temp <= MAX_OFFER_RATE) {
                    value = temp;
                    return true;
                }
                else {
                    value = uint.MaxValue;
                    return false;
                }
            }
            else {
                value = uint.MaxValue;
                return false;
            }
            
        }


        // PRIVATE METHODS //
        private static void _printUsageMsg()
        {
 	        string[] msg = {"Please provide valid arguments.",
                            "usage: Quote.exe \"file_name.csv\" xxx",
                            "where:",
                            "loans.csv - path to file containing loan offers in CSV format. Please use \"\" quotes if file name(path) has spaces",
                            "xxx - loan ammount in GBP in range from £1000 to £15000 in £100 pounds increments"};
            _ConsoleMsg(msg);

        }

        private static void _ConsoleMsg(string[] msg)
        {
 	        foreach (string line in msg) {
                Console.WriteLine(line);
            }
        }

        private static bool _checkArgsIn(string[] args)
        {
            int numArgs = args.Length;

            if (numArgs >2 || numArgs == 0 ) {
                return false;
            
            }
            else if (!_parseRequestedAmmount(args[1])) {
                return false;
                
            }
            else {
                string csvFileName = args[0];

                if (File.Exists(csvFileName))
                {
                    return _readInputData(csvFileName);
                }
                else {

                    return false;
                }

            }

            
        }

        private static bool _parseRequestedAmmount(string input)
        {
            bool status = uint.TryParse(input, out LoanRequested);
            if (LoanRequested <= MAX_LOAN_REQUEST && LoanRequested >= MIN_LOAN_REQUEST && LoanRequested % LOAN_REQUEST_INC == 0)
            {
                status = true;
            }
            else {
                status = false;
            }

            return status;

        }

        private static bool _readInputData(string csvFileName)
        {
            
            
            using (TextFieldParser csv_parser = new TextFieldParser(csvFileName)) {
                
                csv_parser.TextFieldType = FieldType.Delimited;
                csv_parser.SetDelimiters(",");
                csv_parser.TrimWhiteSpace = true;

                // Parse header
                string[] headerRow;
                try
                {
                    headerRow = csv_parser.ReadFields();
                }
                catch (MalformedLineException ex) {

                    Console.WriteLine("Line" + ex.Message + "is not valid in file: " + csvFileName);
                    return false;
                    
                }

                if (headerRow.Length != COLUMN_NAMES.Length) {
                    return false;
                }

                // check for column names
                for (int ii = 0; ii < COLUMN_NAMES.Length; ii++) { 
                    if (!(headerRow[ii].Equals(COLUMN_NAMES[ii]))) {
                        return false;
                    }
                }

                // create table columns
                _addTableColumns();

                bool status = false;

                while (!csv_parser.EndOfData)
                    {

                        string[] fieldRow;
                        // read next row of data
                        try
                        {
                            fieldRow = csv_parser.ReadFields();
                        }
                        catch (MalformedLineException ex)
                        {
                            Console.WriteLine("Line" + ex.Message + "is not valid in file: " + csvFileName);
                            return false;

                        }
                        
                        if (fieldRow.Length != COLUMN_NAMES.Length) {
                            return false;
                        }

                        status = _AddTableRow(fieldRow);

                        if (!status) {

                            string[] msg = { "Problem in file: ", csvFileName, "row contents: ", string.Join(",", fieldRow), "\n"};
                            _ConsoleMsg(msg);

                            return status; } 
                    }
                
                return status;
 
            }

        }

        private static bool _AddTableRow(string[] fieldRow)
        {
            
            DataRow newRow = lendingOffersTable.NewRow();
            bool status = false;

            for (int ii = 0; ii < COLUMN_NAMES.Length; ii++ ) {

                string input = fieldRow[ii];
                ParserDel parser = (ParserDel)rowParsers[COLUMN_NAMES[ii]];
                    object value;

                    status = parser(input, out value);
                    if (status)
                    {
                        newRow[COLUMN_NAMES[ii]] = value;
                    }
                    else {
                        return false;
                    }
             }

            // Add the row to the rows collection.
            lendingOffersTable.Rows.Add(newRow);

            return status;

        }

        private static void _addTableColumns()
        {
            for (int ii = 0; ii < COLUMN_NAMES.Length; ii++ )
            {

                DataColumn oDataColumn = new DataColumn(COLUMN_NAMES[ii], COLUMN_TYPES[ii]);
                //setting the default value of empty.string to newly created column
                
                lendingOffersTable.Columns.Add(oDataColumn);
                
            }
        }
    }
}
