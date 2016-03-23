    $(document).ready(function () {
 
        //Prepare jtable plugin
        $('#TestTableContainer').jtable({
            title: 'Server overview',
            paging: false, //Enables paging
            // pageSize: 10, //Actually this is not needed since default value is 10.
            sorting: false, //Enables sorting
            //defaultSorting: 'Name ASC', //Optional. Default sorting on first load.
/*
            actions: {
                listAction: '/PagingAndSorting.aspx/StudentList',
                createAction: '/PagingAndSorting.aspx/CreateStudent',
                updateAction: '/PagingAndSorting.aspx/UpdateStudent',
                deleteAction: '/PagingAndSorting.aspx/DeleteStudent'
            },
*/
            fields: {
                identifier: {
                    title: 'identifier',
                    width: '23%',
                    key: true,
                    // create: true,
                    // edit: false,
                    // list: false
                },
                ip: {
                    title: 'IP',
                    width: '23%'
                },
                ping: {
                    title: 'PING',
                    width: '23%'
        }
    }
        });

        // Populate table
        /*
        $('#TestTableContainer').jtable('addRecord', {
            record: {
                identifier: 'abcd',
                ip: '1.1.1.1',
            },
            clientOnly: true
        });
        */
        //Load student list from server
        // $('#StudentTableContainer').jtable('load');
    });