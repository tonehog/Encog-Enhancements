Encog-Enhancements

DukascopyLoader -
	Loads FX data (world indices and other data later to come) from Dukascopy
	and (eventually) will continue querying until fromDate and toDate is filled
	with data. Currently usese a Dictionary<string, int> as a table for the
	query, but may perhaps be later changed to a public enum. Currently need to
	view the source to see current traded currency pairs. Addition of enum will
	later make this more simple. 
